using BWB_Auswertung.Models;
using Renci.SshNet;

namespace BWB_Auswertung.IO
{
    /// <summary>
    /// Zentrale Stelle zum Aufbauen von SFTP-Verbindungen.
    /// Bietet sowohl die klassische <c>password</c>-Methode als auch
    /// <c>keyboard-interactive</c> an, weil viele OpenSSH-Server (z.B. bei
    /// gängigen Hostern) Passwort-Logins nur noch über <c>keyboard-interactive</c>
    /// erlauben. Die Beantwortung der Prompts erfolgt automatisch mit dem
    /// hinterlegten Passwort – es ist keine Nutzerinteraktion nötig.
    /// </summary>
    public static class SftpFactory
    {
        public static SftpClient Create(Settings settings, int port = 22)
        {
            return new SftpClient(BuildConnectionInfo(settings, port));
        }

        public static ConnectionInfo BuildConnectionInfo(Settings settings, int port = 22)
        {
            var passwordAuth = new PasswordAuthenticationMethod(settings.Username, settings.Password);

            var keyboardAuth = new KeyboardInteractiveAuthenticationMethod(settings.Username);
            keyboardAuth.AuthenticationPrompt += (sender, args) =>
            {
                foreach (var prompt in args.Prompts)
                {
                    prompt.Response = settings.Password;
                }
            };

            return new ConnectionInfo(settings.Hostname, port, settings.Username,
                passwordAuth, keyboardAuth);
        }

        /// <summary>
        /// Prüft, ob eine Datei auf dem SFTP-Server existiert.
        /// </summary>
        public static bool FileExists(Settings settings, string remotePath)
        {
            using var sftp = Create(settings);
            sftp.Connect();
            bool exists = sftp.Exists(remotePath);
            sftp.Disconnect();
            return exists;
        }

        /// <summary>
        /// Löscht eine Datei auf dem SFTP-Server, falls sie existiert.
        /// </summary>
        public static void DeleteFile(Settings settings, string remotePath)
        {
            using var sftp = Create(settings);
            sftp.Connect();
            if (sftp.Exists(remotePath))
                sftp.DeleteFile(remotePath);
            sftp.Disconnect();
        }
    }
}
