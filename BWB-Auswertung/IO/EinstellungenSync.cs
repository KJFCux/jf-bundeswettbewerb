using System;
using System.IO;
using System.Xml.Serialization;
using BWB_Auswertung.Models;
using Renci.SshNet;

namespace BWB_Auswertung.IO
{
    /// <summary>
    /// Teilt die Veranstaltungs-Einstellungen über den SFTP-Server, damit sie nur an einem Rechner
    /// eingetragen werden müssen. Die Datei liegt im Unterordner "Einstellungen" des SFTP-Pfades
    /// (Unterordner werden beim Gruppen-Sync ignoriert).
    ///
    /// Nicht übertragen werden:
    /// - SFTP-Zugangsdaten (Server, Pfad, Benutzer, Passwort). Der andere Rechner braucht sie ohnehin
    ///   zum Verbinden, und so landet das Passwort nicht auf dem Server.
    /// - Unterschriften (Bilddateien). Diese bleiben lokal, damit keine Unterschriften auf dem Server liegen.
    /// Das Logo wird als Datei mit hochgeladen.
    /// </summary>
    public static class EinstellungenSync
    {
        private const string RemoteOrdnerName = "Einstellungen";
        private const string RemoteDateiName = "einstellungen.xml";
        private const string RemoteLogoBasisName = "logo";

        private static string RemoteOrdner(Settings settings) => $"{settings.Pfad}/{RemoteOrdnerName}";

        public static void Hochladen(Settings lokal)
        {
            Settings kopie = Klonen(lokal);
            kopie.Hostname = string.Empty;
            kopie.Pfad = string.Empty;
            kopie.Username = string.Empty;
            kopie.Password = string.Empty;
            kopie.Unterschriftlinks = string.Empty;
            kopie.Unterschriftrechts = string.Empty;
            kopie.Logopfad = string.Empty;

            using SftpClient sftp = SftpFactory.Create(lokal);
            sftp.Connect();

            string ordner = RemoteOrdner(lokal);
            if (!sftp.Exists(ordner))
            {
                sftp.CreateDirectory(ordner);
            }

            if (!string.IsNullOrWhiteSpace(lokal.Logopfad) && File.Exists(lokal.Logopfad))
            {
                string logoName = RemoteLogoBasisName + Path.GetExtension(lokal.Logopfad).ToLowerInvariant();
                using (FileStream logo = File.OpenRead(lokal.Logopfad))
                {
                    sftp.UploadFile(logo, $"{ordner}/{logoName}", true);
                }
                //Auf dem Server nur der Dateiname, beim Herunterladen wird der lokale Pfad gesetzt
                kopie.Logopfad = logoName;
            }

            using (var stream = new MemoryStream())
            {
                new XmlSerializer(typeof(Settings)).Serialize(stream, kopie);
                stream.Position = 0;
                sftp.UploadFile(stream, $"{ordner}/{RemoteDateiName}", true);
            }

            sftp.Disconnect();
        }

        /// <summary>
        /// Lädt die Einstellungen vom Server und führt sie mit den lokalen zusammen.
        /// Gibt null zurück, wenn auf dem Server noch keine Einstellungen liegen.
        /// </summary>
        /// <param name="lokal">Aktuelle lokale Einstellungen (liefert die Zugangsdaten)</param>
        /// <param name="lokalerOrdner">Ordner, in dem ein mitgeliefertes Logo gespeichert wird</param>
        public static Settings? Herunterladen(Settings lokal, string lokalerOrdner)
        {
            using SftpClient sftp = SftpFactory.Create(lokal);
            sftp.Connect();

            string ordner = RemoteOrdner(lokal);
            string remoteDatei = $"{ordner}/{RemoteDateiName}";
            if (!sftp.Exists(remoteDatei))
            {
                sftp.Disconnect();
                return null;
            }

            Settings remote;
            using (var stream = new MemoryStream())
            {
                sftp.DownloadFile(remoteDatei, stream);
                stream.Position = 0;
                remote = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream)!;
            }

            //Zugangsdaten und Unterschriften immer lokal behalten
            remote.Hostname = lokal.Hostname;
            remote.Pfad = lokal.Pfad;
            remote.Username = lokal.Username;
            remote.Password = lokal.Password;
            remote.Unterschriftlinks = lokal.Unterschriftlinks;
            remote.Unterschriftrechts = lokal.Unterschriftrechts;

            //Logo: vom Server holen, falls mitgeliefert, sonst das lokale behalten
            string? logoName = string.IsNullOrWhiteSpace(remote.Logopfad) ? null : Path.GetFileName(remote.Logopfad);
            remote.Logopfad = lokal.Logopfad;
            if (logoName != null && sftp.Exists($"{ordner}/{logoName}"))
            {
                Directory.CreateDirectory(lokalerOrdner);
                string lokalesLogo = Path.Combine(lokalerOrdner, logoName);
                using (FileStream fs = File.Create(lokalesLogo))
                {
                    sftp.DownloadFile($"{ordner}/{logoName}", fs);
                }
                remote.Logopfad = lokalesLogo;
            }

            sftp.Disconnect();
            return remote;
        }

        private static Settings Klonen(Settings settings)
        {
            var serializer = new XmlSerializer(typeof(Settings));
            using var stream = new MemoryStream();
            serializer.Serialize(stream, settings);
            stream.Position = 0;
            return (Settings)serializer.Deserialize(stream)!;
        }
    }
}
