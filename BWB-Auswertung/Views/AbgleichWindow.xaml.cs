using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MahApps.Metro.Controls;
using BWB_Auswertung.Models;
using BWB_Auswertung.Network;

namespace BWB_Auswertung.Views
{
    public partial class AbgleichWindow : MetroWindow
    {
        private readonly Func<IEnumerable<Gruppe>> lokalGruppenProvider;
        private readonly string lokalerVeranstaltungstitel;
        private readonly PeerDiscovery? discovery;
        private List<GruppenVergleich> aktuellesErgebnis = new List<GruppenVergleich>();
        private Peer? gewaehlterPeer;
        private string? manuellHost;
        private int? manuellPort;

        public AbgleichWindow(PeerDiscovery? discovery,
                              Func<IEnumerable<Gruppe>> lokalGruppenProvider,
                              string lokalerVeranstaltungstitel,
                              int defaultSyncPort)
        {
            InitializeComponent();
            this.discovery = discovery;
            this.lokalGruppenProvider = lokalGruppenProvider;
            this.lokalerVeranstaltungstitel = lokalerVeranstaltungstitel ?? string.Empty;
            this.manuellPort = defaultSyncPort;

            if (discovery != null)
            {
                PeerListBox.ItemsSource = discovery.Peers;
            }
            else
            {
                PeerListBox.ItemsSource = new ObservableCollection<Peer>();
                StatusText.Text = "LAN-Abgleich ist in den Einstellungen deaktiviert.";
            }
        }

        private void PeerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gewaehlterPeer = PeerListBox.SelectedItem as Peer;
            AbgleichButton.IsEnabled = gewaehlterPeer != null;

            if (gewaehlterPeer != null
                && !string.IsNullOrWhiteSpace(gewaehlterPeer.Veranstaltungstitel)
                && !string.IsNullOrWhiteSpace(lokalerVeranstaltungstitel)
                && !string.Equals(gewaehlterPeer.Veranstaltungstitel.Trim(), lokalerVeranstaltungstitel.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                StatusText.Text = $"⚠ Partner-PC hat anderen Veranstaltungstitel: \"{gewaehlterPeer.Veranstaltungstitel}\"";
            }
            else if (gewaehlterPeer != null)
            {
                StatusText.Text = $"Bereit für Abgleich mit {gewaehlterPeer.Hostname} ({gewaehlterPeer.IpAddress}).";
            }
        }

        private async void AbgleichButton_Click(object sender, RoutedEventArgs e)
        {
            if (gewaehlterPeer == null) return;
            await StartAbgleichAsync(gewaehlterPeer.IpAddress, gewaehlterPeer.SyncPort);
        }

        private async void ManualConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ManuelleVerbindungDialog
            {
                Owner = this,
                HostInput = manuellHost ?? string.Empty,
                PortInput = (manuellPort ?? 47801).ToString()
            };
            if (dialog.ShowDialog() == true)
            {
                manuellHost = dialog.HostInput;
                if (int.TryParse(dialog.PortInput, out int p)) manuellPort = p;
                if (!string.IsNullOrWhiteSpace(manuellHost) && manuellPort.HasValue)
                {
                    await StartAbgleichAsync(manuellHost!, manuellPort.Value);
                }
            }
        }

        private async System.Threading.Tasks.Task StartAbgleichAsync(string host, int port)
        {
            StatusText.Text = $"Verbinde zu {host}:{port}...";
            AbgleichButton.IsEnabled = false;
            ManualConnectButton.IsEnabled = false;
            AktualisierenButton.IsEnabled = false;

            try
            {
                SyncResultDto partner = await SyncClient.FetchGruppenAsync(host, port).ConfigureAwait(true);
                var lokal = lokalGruppenProvider().ToList();
                aktuellesErgebnis = GruppenComparer.Compare(lokal, partner.Gruppen);

                int abw = aktuellesErgebnis.Count(g => g.Status == VergleichStatus.Abweichend);
                int nurLokal = aktuellesErgebnis.Count(g => g.Status == VergleichStatus.NurLokal);
                int nurPartner = aktuellesErgebnis.Count(g => g.Status == VergleichStatus.NurPartner);
                int identisch = aktuellesErgebnis.Count(g => g.Status == VergleichStatus.Identisch);

                StatusText.Text = $"Abgleich mit {partner.Hostname}: {identisch} identisch, {abw} abweichend, {nurLokal} nur lokal, {nurPartner} nur Partner.";
                AktualisierenButton.IsEnabled = true;
                ApplyFilterAndBind();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Fehler: {ex.Message}";
                MessageBox.Show($"Abgleich fehlgeschlagen:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                AbgleichButton.IsEnabled = gewaehlterPeer != null;
                ManualConnectButton.IsEnabled = true;
            }
        }

        private void ApplyFilterAndBind()
        {
            IEnumerable<GruppenVergleich> daten = aktuellesErgebnis;
            if (NurAbweichungenCheckBox.IsChecked == true)
            {
                daten = daten.Where(d => d.Status != VergleichStatus.Identisch);
            }
            VergleichGrid.ItemsSource = daten.ToList();
            DetailGrid.ItemsSource = null;
        }

        private void FilterChanged(object sender, RoutedEventArgs e) => ApplyFilterAndBind();

        private void VergleichGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VergleichGrid.SelectedItem is GruppenVergleich gv)
            {
                DetailGrid.ItemsSource = gv.Differenzen;
            }
            else
            {
                DetailGrid.ItemsSource = null;
            }
        }

        private async void AktualisierenButton_Click(object sender, RoutedEventArgs e)
        {
            if (gewaehlterPeer != null)
            {
                await StartAbgleichAsync(gewaehlterPeer.IpAddress, gewaehlterPeer.SyncPort);
            }
            else if (!string.IsNullOrWhiteSpace(manuellHost) && manuellPort.HasValue)
            {
                await StartAbgleichAsync(manuellHost!, manuellPort.Value);
            }
        }

        private void SchliessenButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
