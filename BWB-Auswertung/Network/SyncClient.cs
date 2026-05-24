using System;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BWB_Auswertung.Network
{
    public static class SyncClient
    {
        public static async Task<SyncResultDto> FetchGruppenAsync(string host, int port, int connectTimeoutMs = 5000, int readTimeoutMs = 30000)
        {
            using var tcp = new TcpClient();
            using var cts = new CancellationTokenSource(connectTimeoutMs);
            try
            {
                await tcp.ConnectAsync(host, port).WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Verbindung zu {host}:{port} hat das Zeitlimit ({connectTimeoutMs} ms) überschritten");
            }

            tcp.ReceiveTimeout = readTimeoutMs;
            tcp.SendTimeout = readTimeoutMs;

            using var stream = tcp.GetStream();
            string reqJson = JsonSerializer.Serialize(new SyncRequest(), SyncProtocol.JsonOptions);
            await SyncProtocol.WriteFrameAsync(stream, reqJson).ConfigureAwait(false);

            string respJson = await SyncProtocol.ReadFrameAsync(stream).ConfigureAwait(false);
            SyncResponse? resp = JsonSerializer.Deserialize<SyncResponse>(respJson, SyncProtocol.JsonOptions);
            if (resp == null) throw new InvalidOperationException("Leere Antwort vom Partner-PC");
            if (!string.IsNullOrEmpty(resp.Fehler))
            {
                throw new InvalidOperationException($"Fehler auf Partner-PC: {resp.Fehler}");
            }

            return new SyncResultDto
            {
                Hostname = resp.Hostname,
                Veranstaltungstitel = resp.Veranstaltungstitel,
                Zeitpunkt = resp.Zeitpunkt,
                Gruppen = resp.Gruppen
            };
        }
    }

    public class SyncResultDto
    {
        public string Hostname { get; set; } = string.Empty;
        public string Veranstaltungstitel { get; set; } = string.Empty;
        public DateTime Zeitpunkt { get; set; }
        public Models.Gruppe[] Gruppen { get; set; } = Array.Empty<Models.Gruppe>();
    }
}
