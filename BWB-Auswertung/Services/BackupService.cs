using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Threading;
using BWB_Auswertung.IO;

namespace BWB_Auswertung.Services
{
    public class BackupService
    {
        private readonly string dataPath;
        private readonly string settingsPath;
        private readonly string backupPath;
        private const int MaxBackups = 30;
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

        private DispatcherTimer? timer;

        public BackupService(string dataPath, string settingsPath, string backupPath)
        {
            this.dataPath = dataPath;
            this.settingsPath = settingsPath;
            this.backupPath = backupPath;
        }

        public void Start()
        {
            Directory.CreateDirectory(backupPath);
            timer = new DispatcherTimer { Interval = Interval };
            timer.Tick += (_, __) => RunBackupSafe();
            timer.Start();
        }

        public void Stop()
        {
            timer?.Stop();
            timer = null;
        }

        private void RunBackupSafe()
        {
            try
            {
                CreateBackup();
                CleanupOldBackups();
            }
            catch (Exception ex)
            {
                LOGGING.Write($"Backup fehlgeschlagen: {ex.Message}",
                    System.Reflection.MethodBase.GetCurrentMethod()!.Name,
                    System.Diagnostics.EventLogEntryType.Warning);
            }
        }

        private void CreateBackup()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string zipFile = Path.Combine(backupPath, $"Backup_{timestamp}.zip");
            string tmpFile = zipFile + ".tmp";

            using (var fs = new FileStream(tmpFile, FileMode.Create))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                AddDirectoryToZip(zip, dataPath, "Gruppendaten");
                AddDirectoryToZip(zip, settingsPath, "Einstellungen");
            }
            File.Move(tmpFile, zipFile);
        }

        private static void AddDirectoryToZip(ZipArchive zip, string sourceDir, string entryRoot)
        {
            if (!Directory.Exists(sourceDir)) return;
            foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
                string entryName = $"{entryRoot}/{rel}";
                try
                {
                    zip.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
                }
                catch (IOException)
                {
                    // Datei gerade gesperrt -> überspringen
                }
            }
        }

        private void CleanupOldBackups()
        {
            var files = new DirectoryInfo(backupPath)
                .GetFiles("Backup_*.zip")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ToList();
            foreach (var old in files.Skip(MaxBackups))
            {
                try { old.Delete(); } catch { /* ignore */ }
            }
        }
    }
}
