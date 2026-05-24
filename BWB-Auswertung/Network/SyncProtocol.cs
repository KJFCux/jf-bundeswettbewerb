using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace BWB_Auswertung.Network
{
    internal static class SyncProtocol
    {
        public const int MaxPayloadBytes = 64 * 1024 * 1024; //64 MB Sicherheitsobergrenze

        public static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(typeInfo =>
            {
                if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
                //Read-only (berechnete) Properties komplett auslassen.
                //Diese werfen teils Exceptions (z.B. Gruppe.GesamtAlter greift auf Persons[0..8].Alter zu)
                //und sollen ohnehin auf dem Empfänger neu berechnet werden.
                for (int i = typeInfo.Properties.Count - 1; i >= 0; i--)
                {
                    if (typeInfo.Properties[i].Set == null)
                    {
                        typeInfo.Properties.RemoveAt(i);
                    }
                }
            });

            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = false,
                IncludeFields = false,
                TypeInfoResolver = resolver,
                //Sicherheitsnetz: einzelne fehlerhafte Property-Werte (z.B. null bei required) sollen
                //den ganzen Snapshot nicht killen – kommt in der Praxis aber nur sehr selten vor.
            };
        }

        public static async Task WriteFrameAsync(NetworkStream stream, string json)
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            byte[] header = new byte[4];
            //Big-Endian Länge
            header[0] = (byte)((payload.Length >> 24) & 0xFF);
            header[1] = (byte)((payload.Length >> 16) & 0xFF);
            header[2] = (byte)((payload.Length >> 8) & 0xFF);
            header[3] = (byte)(payload.Length & 0xFF);
            await stream.WriteAsync(header, 0, 4).ConfigureAwait(false);
            await stream.WriteAsync(payload, 0, payload.Length).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        public static async Task<string> ReadFrameAsync(NetworkStream stream)
        {
            byte[] header = await ReadExactAsync(stream, 4).ConfigureAwait(false);
            int len = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
            if (len < 0 || len > MaxPayloadBytes)
                throw new InvalidDataException($"Ungültige Frame-Länge: {len}");
            byte[] payload = await ReadExactAsync(stream, len).ConfigureAwait(false);
            return Encoding.UTF8.GetString(payload);
        }

        private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int count)
        {
            byte[] buf = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buf, offset, count - offset).ConfigureAwait(false);
                if (read <= 0) throw new EndOfStreamException("Verbindung wurde beendet");
                offset += read;
            }
            return buf;
        }
    }

    internal class SyncRequest
    {
        public string Command { get; set; } = "GET_GRUPPEN";
    }

    internal class SyncResponse
    {
        public string Hostname { get; set; } = string.Empty;
        public string Veranstaltungstitel { get; set; } = string.Empty;
        public DateTime Zeitpunkt { get; set; }
        public Models.Gruppe[] Gruppen { get; set; } = Array.Empty<Models.Gruppe>();
        public string? Fehler { get; set; }
    }
}
