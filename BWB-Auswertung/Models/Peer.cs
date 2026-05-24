using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BWB_Auswertung.Models
{
    public class Peer : INotifyPropertyChanged
    {
        private string hostname = string.Empty;
        private string ipAddress = string.Empty;
        private int syncPort;
        private string veranstaltungstitel = string.Empty;
        private DateTime lastSeen;

        public string Hostname
        {
            get => hostname;
            set { hostname = value; OnPropertyChanged(); }
        }

        public string IpAddress
        {
            get => ipAddress;
            set { ipAddress = value; OnPropertyChanged(); }
        }

        public int SyncPort
        {
            get => syncPort;
            set { syncPort = value; OnPropertyChanged(); }
        }

        public string Veranstaltungstitel
        {
            get => veranstaltungstitel;
            set { veranstaltungstitel = value; OnPropertyChanged(); OnPropertyChanged(nameof(Anzeige)); }
        }

        public DateTime LastSeen
        {
            get => lastSeen;
            set { lastSeen = value; OnPropertyChanged(); }
        }

        public string Anzeige => $"{Hostname} ({IpAddress})" + (string.IsNullOrWhiteSpace(Veranstaltungstitel) ? string.Empty : $" – {Veranstaltungstitel}");

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
