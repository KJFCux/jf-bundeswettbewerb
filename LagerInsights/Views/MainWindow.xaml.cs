using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using LagerInsights.IO;
using LagerInsights.Models;
using LagerInsights.Views;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Renci.SshNet;
using Settings = LagerInsights.Properties.Settings;

namespace LagerInsights;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    private readonly string AppDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string dataPath;
    private readonly string ProgrammName = AppDomain.CurrentDomain.FriendlyName;
    private readonly string settingsPath;
    private string currentVersion = "";

    public MainWindow()
    {
        //Alles umstellen auf Deutsche Lokalisierung
        var vCulture = new CultureInfo("de-DE");

        Thread.CurrentThread.CurrentCulture = vCulture;
        Thread.CurrentThread.CurrentUICulture = vCulture;
        CultureInfo.DefaultThreadCurrentCulture = vCulture;
        CultureInfo.DefaultThreadCurrentUICulture = vCulture;
        LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(
            XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));


        //Initialize
        InitializeComponent();
        DataContext = new MainViewModel();

        //Speicherort für alle Gruppendaten festlegen und evtl. Ordner erstellen
        dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ProgrammName,
            "Gruppendaten");
        _ = Directory.CreateDirectory(dataPath);

        LOGGING.Write(dataPath, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);


        //Speicherort für Logdateien und evtl. Ordner erstellen
        _ = Directory.CreateDirectory(Path.Combine(AppDataLocal, ProgrammName, "log"));

        //Beim Starten des Programms evtl. Bereits vorhandene Gruppen wieder in das Programm laden
        LoadData();

        //Einstellungen Laden
        settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ProgrammName,
            "Einstellungen");
        _ = Directory.CreateDirectory(settingsPath);
        LoadSettings();

        //Prüfe auf Updates
        _ = CheckForUpdate();
        gruppenListBox.SelectedIndex = 0;
        gruppenListBox.Focus();
    }

    public async Task<bool> CheckForUpdate()
    {
        try
        {
            //Aktuelle Version nach Start des Programms setzen und auf Updates prüfen
            var assembly = Assembly.GetExecutingAssembly();
            var versionInfo = assembly.GetName().Version;
            currentVersion = $"v{versionInfo.Major}.{versionInfo.MajorRevision}.{versionInfo.Build}";

            // HTTP Client erstellen und den aktuellen ReleaseTag von Github abfragen
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "UpdateChecker");

                var response = await client.GetAsync(Settings.Default.GithubReleaseURL);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                List<GitHub> releases = JsonSerializer.Deserialize<List<GitHub>>(responseBody);

                // Letzten release ermitteln
                var latestRelease = releases[0];

                // Mit lokaler Version vergleichen
                if (latestRelease.tag_name.CompareTo($"LagerInsights/{currentVersion}") > 0)
                {
                    // Eine neue Version ist verfügbar. Fragen ob die Datei heruntergeladen werden soll
                    var result =
                        MessageBox.Show(
                            "Es gibt eine neue Version der LagerInsights!\nMöchtest du die neue Version herunterladen?",
                            "Update verfügbar!", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                        Process.Start(
                            new ProcessStartInfo(
                                    Settings.Default.GithubDownloadURL.Replace("{release}", latestRelease.tag_name))
                            { UseShellExecute = true });
                }
            }
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }

        return false;
    }

    private void LaunchWebsite_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Settings.Default.BWBURL) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Webseitenaufruf fehlgeschlagen\n{ex}", "Fehler: Webseitenaufruf", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Github_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Settings.Default.GithubURL) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Webseitenaufruf fehlgeschlagen\n{ex}", "Fehler: Webseitenaufruf", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    private void About_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Settings.Default.BWBURL) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Webseitenaufruf fehlgeschlagen\n{ex}", "Fehler: Webseitenaufruf", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Hilfe_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Settings.Default.BWBURL) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Webseitenaufruf fehlgeschlagen\n{ex}", "Fehler: Webseitenaufruf", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "LagerInsights Dateien (*.XML)|*.xml|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
                foreach (var file in openFileDialog.FileNames)
                    OpenDeserializerForFile(file, true);

            var viewModel = (MainViewModel)DataContext;
            //Filterung entfernen
            FertigeGruppenAusblenden_Checkbox.IsChecked = false;

            //Importiertes einsortieren
            viewModel.Sort(sortComboBox.SelectedIndex);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Import fehlgeschlagen\n{ex}", "Fehler: Import", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            //Speichern an dem in der DialogBox angegebenen Ort
            SaveData(new Dialogs().ShowFolderBrowserDialog());
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Export fehlgeschlagen\n{ex}", "Fehler: Export", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenEvaluation_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var viewModel = (MainViewModel)DataContext;
            if (viewModel.Gruppen.Count == 0)
            {
                MessageBox.Show("Keine Gruppen vorhanden", "Auswertung kann nicht geöffnet werden", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            //Filterung entfernen
            FertigeGruppenAusblenden_Checkbox.IsChecked = false;
            //Vor dem öffnen der Auswertung die Liste neu sortieren um evtl.
            //Platz Änderungen korrekt zu setzen
            viewModel.Sort(sortComboBox.SelectedIndex);
            var neuesFenster = new EvaluationView(viewModel);
            neuesFenster.ShowDialog();
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Öffen der Auswertung fehlgeschlagen\n{ex}", "Fehler: Öffne Auswertung",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void version_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var message = $"{ProgrammName} {currentVersion}";
            MessageBox.Show(message, "Version", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Ermitteln der Version fehlgeschlagen\n{ex}", "Fehler: Version", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    private void gruppenListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var viewModel = (MainViewModel)DataContext;

            // Stellen Sie sicher, dass ein Element ausgewählt ist und es sich um eine Gruppe handelt
            if (gruppenListBox.SelectedItem != null && gruppenListBox.SelectedItem is Jugendfeuerwehr selectedGruppe)
                // Der DataContext wird automatisch aktualisiert
                //Bei Gruppenwechsel der jeweiligen Gruppe den aktuellen Teilnehmerbeitrag setzen
                selectedGruppe.Teilnehmerbeitrag = viewModel.Einstellungen.Teilnehmendenbeitrag;
            //Wenn die Sortierung einen Wert hat, danach sortieren

            //Alles neu sortieren
            viewModel.Sort(sortComboBox.SelectedIndex);

            //Beim Durchklicken durch die Gruppen alle Daten speichern
            //Beim Durchklicken sind evtl. Bearbeitungen abgeschlossen. Daher alles speichern
            SaveData(dataPath, true);
            FertigeGruppenAusblendenHelper();
            gruppenListBox.Focus();
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Fehler bei Gruppenauswahl\n{ex}", "Fehler: Gruppenauswahl", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void sortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            // Stellen Sie sicher, dass ein Element ausgewählt ist und es sich um eine Gruppe handelt
            if (gruppenListBox != null)
            {
                if (gruppenListBox.SelectedItem != null &&
                    gruppenListBox.SelectedItem is Jugendfeuerwehr selectedGruppe)
                {
                    // Der DataContext wird automatisch aktualisiert
                }

                var viewModel = (MainViewModel)DataContext;
                viewModel.Sort(sortComboBox.SelectedIndex);
                FertigeGruppenAusblendenHelper();
                gruppenListBox.Focus();
            }
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim sortieren der Gruppen\n{ex}", "Fehler: Gruppensortierung",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void gruppenListBox_KeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (gruppenListBox.SelectedIndex == -1) return;

            switch (e.Key)
            {
                case Key.Up:
                    if (gruppenListBox.SelectedIndex > 0)
                    {
                        gruppenListBox.SelectedIndex--;
                        e.Handled = true; // Verhindert das Standardverhalten der ListBox
                    }

                    break;
                case Key.Down:
                    if (gruppenListBox.SelectedIndex < gruppenListBox.Items.Count - 1)
                    {
                        gruppenListBox.SelectedIndex++;
                        e.Handled = true; // Verhindert das Standardverhalten der ListBox
                    }

                    break;
            }
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim durschalten der Gruppen\n{ex}", "Fehler: Gruppen", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void gruppenListBox_Loaded(object sender, RoutedEventArgs e)
    {
        gruppenListBox.Focus();
    }

    //Hinzufügen einer leeren neuen Gruppe
    private void ButtonAddGroup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var viewModel = (MainViewModel)DataContext;
            viewModel.AddEmptyGruppe("Feuerwehr");
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim hinzufügen einer neuen Feuerwehr\n{ex}", "Fehler: Neue Feuerwehr",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    //Anzeigen der globalen Einstellungen
    private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var neuesFenster = new SettingsWindow();
            var result = neuesFenster.ShowDialog();
            LoadSettings();
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim öffnen der Einstellungen\n{ex}", "Fehler: Öffne Einstellungen",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    //Beim schließen des Programms alle Daten speichern
    private void Window_Closing(object sender, CancelEventArgs e)
    {
        try
        {
            SaveData(dataPath, true);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim speichern\n{ex}", "Fehler: Speichern", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    //Auf drücken von Strg+S alles speichern
    private void CommandSaving(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            SaveData(dataPath, true);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim speichern\n{ex}", "Fehler: Speichern", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CommandSaving_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }


    //Speichert alle Daten als einzelne XML Dateien
    private void SaveData(string savePath, bool deleteOld = false)
    {
        try
        {
            List<string> aktuelleDateien = new();
            var viewModel = (MainViewModel)DataContext;

            foreach (var gruppe in viewModel.Gruppen)
            {
                var datei = Path.Combine($"{gruppe.FeuerwehrOhneSonderzeichen}.xml");
                WriteFile.writeText(Path.Combine(savePath, datei), SerializeXML<Jugendfeuerwehr>.Serialize(gruppe));

                //Dateinamen merken um alte löschen zu können
                aktuelleDateien.Add(datei);
            }

            // Nicht mehr benötigte Dateien löschen
            if (deleteOld) DeleteFiles.DeleteFilesExcept(aktuelleDateien, savePath);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }

    //Dateien aus dem Ordner Ordner laden
    private void LoadData()
    {
        try
        {
            string[] xmlFiles = Directory.GetFiles(dataPath, "*.xml");
            foreach (var file in xmlFiles) OpenDeserializerForFile(file);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }

    private void OpenDeserializerForFile(string file, bool ueberrschreiben = false, bool showOverrideInfo = true)
    {
        try
        {
            var viewModel = (MainViewModel)DataContext;

            // Deserialisieren der XML-Datei und Hinzufügen der deserialisierten Gruppen zum ViewModel
            var jugendfeuerwehr = DeserializeXML<Jugendfeuerwehr>.Deserialize<Jugendfeuerwehr>(file);

            if (jugendfeuerwehr != null)
            {
                //Schauen ob bereits vorhanden und alten Eintrag löschen
                if (ueberrschreiben)
                {
                    var gefundeneGruppen = viewModel.Gruppen.Where(x => x.Feuerwehr.Equals(jugendfeuerwehr.Feuerwehr))
                        .ToList();

                    foreach (var gefundeneGruppe in gefundeneGruppen)
                    {
                        // Der gezahlte Betrag soll nur überschrieben werden, wenn der Zeitstempel neuer ist
                        if (gefundeneGruppe.TimeStampGezahlterBeitrag != null &&
                            (jugendfeuerwehr.TimeStampGezahlterBeitrag == null || gefundeneGruppe.TimeStampGezahlterBeitrag > jugendfeuerwehr.TimeStampGezahlterBeitrag))
                        {
                            jugendfeuerwehr.GezahlterBeitrag = gefundeneGruppe.GezahlterBeitrag;
                        }

                        // Das Zeltdorf soll nur überschrieben werden, wenn der Zeitstempel neuer ist
                        if (gefundeneGruppe.TimeStampZeltdorf != null &&
                            (jugendfeuerwehr.TimeStampZeltdorf == null || gefundeneGruppe.TimeStampZeltdorf > jugendfeuerwehr.TimeStampZeltdorf))
                        {
                            jugendfeuerwehr.Zeltdorf = gefundeneGruppe.Zeltdorf;
                        }

                        // Die Einverständniserklärung soll nur überschrieben werden, wenn der Zeitstempel neuer ist
                        if (gefundeneGruppe.TimeStampEinverstaendniserklaerung != null &&
                            (jugendfeuerwehr.TimeStampEinverstaendniserklaerung == null || gefundeneGruppe.TimeStampEinverstaendniserklaerung > jugendfeuerwehr.TimeStampEinverstaendniserklaerung))
                        {
                            jugendfeuerwehr.Einverstaendniserklaerung = gefundeneGruppe.Einverstaendniserklaerung;
                        }

                        //Hinweis an Benutzer das die Gruppe existiert
                        if (showOverrideInfo)
                            MessageBox.Show(
                                $"Die JF {jugendfeuerwehr.Feuerwehr} aus {jugendfeuerwehr.Organisationseinheit} Existierte bereits und wurde überschrieben!\nURL der Anmeldung neu: {jugendfeuerwehr.UrlderAnmeldung}\nURL der Anmeldung alt: {gefundeneGruppe.UrlderAnmeldung}",
                                "Anmeldung wurde überschrieben!", MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Alte Gruppe löschen
                        viewModel.RemoveSelectedGroup(gefundeneGruppe, false);
                    }
                }

                //Neuen Eintrag importieren
                viewModel.AddGroup(jugendfeuerwehr);
            }
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }

    private void SyncmitFtp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // SFTP Einstellungen aus dem Model laden
            var viewModel = (MainViewModel)DataContext;
            var einstellungen = viewModel.Einstellungen;

            //Download Pfad erstellen
            var downloadLocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppDomain.CurrentDomain.FriendlyName, "ftp", "download");
            _ = Directory.CreateDirectory(downloadLocalPath);

            //Upload Pfad erstellen
            var uploadlocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppDomain.CurrentDomain.FriendlyName, "ftp", "upload");
            _ = Directory.CreateDirectory(uploadlocalPath);

            // Liste zum Speichern der Dateipfade
            List<string> filePaths = new();

            using (var sftp = new SftpClient(einstellungen.Hostname, 22, einstellungen.Username,
                       einstellungen.Password))
            {
                sftp.Connect();

                var files = sftp.ListDirectory(einstellungen.Pfad);

                //Alle Dateien herunterladen
                foreach (var file in files)
                {
                    if (!file.IsDirectory && file.Name.EndsWith(".xml") &&
                        !file.Name.Contains(
                            "_")) //Mit _ gefolgt von Timestamp werden die Revisionen angelegt. Diese nicht herunterladen.
                    {
                        var localFilePath =
                            Path.Combine(downloadLocalPath, file.Name);
                        using (Stream fileStream = File.Create(localFilePath))
                        {
                            sftp.DownloadFile(file.FullName, fileStream);
                        }

                        filePaths.Add(localFilePath);
                    }
                }

                sftp.Disconnect();
            }

            // Heruntergeladene Daten importieren.
            //Hier wird auch sichergestellt das immer nur die neuesten Werte aus beiden Anmeldungen behalten werden.
            foreach (var file in filePaths) OpenDeserializerForFile(file, true, false);

            // Alle Dateien zum Server hochladen 
            List<string> dateienZumUpload = new List<string>();
            foreach (var gruppe in viewModel.Gruppen)
            {
                string dateiname = gruppe.FeuerwehrOhneSonderzeichen;
                string extracted = ExtractAnmeldungParameter(gruppe.UrlderAnmeldung);
                if (extracted != "")
                    dateiname = extracted;

                var datei = Path.Combine($"{dateiname}.xml");
                WriteFile.writeText(Path.Combine(uploadlocalPath, datei), SerializeXML<Jugendfeuerwehr>.Serialize(gruppe));
                dateienZumUpload.Add(datei);
            }
            using (var sftp = new SftpClient(einstellungen.Hostname, 22, einstellungen.Username,
                       einstellungen.Password))
            {
                sftp.Connect();

                //Alle Dateien hochladen
                foreach (var datei in dateienZumUpload)
                {
                    sftp.UploadFile(File.OpenRead(Path.Combine(uploadlocalPath, datei)),
                        $"{einstellungen.Pfad}/{datei}", true);
                }
                sftp.Disconnect();

            }

            //Filterung entfernen
            FertigeGruppenAusblenden_Checkbox.IsChecked = false;

            //Importiertes einsortieren
            viewModel.Sort(sortComboBox.SelectedIndex);


            MessageBox.Show("Anmeldungen erfolgreich Synchronisiert!", "SFTP Sync erfolgreich", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim Herunterladen der Dateien\n{ex}", "Fehler: SFTP Sync", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private string ExtractAnmeldungParameter(string url)
    {
        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        return query["anmeldung"] ?? "";
    }

    private void LoadSettings()
    {
        try
        {
            //Settings.xml laden
            string[] xmlFile = Directory.GetFiles(settingsPath, "settings.xml");
            if (xmlFile.Length < 1) return;


            var viewModel = (MainViewModel)DataContext;

            // Deserialisieren der XML-Datei und Hinzufügen der deserialisierten Gruppen zum ViewModel
            var einstellungen = DeserializeXML<Models.Settings>.Deserialize<Models.Settings>(xmlFile[0]);
            if (einstellungen != null) viewModel.OverrideSettings(einstellungen);

            //Veranstaltungszeit Global setzen
            Globals.VERANSTALTUNGSDATUM = viewModel.Einstellungen.Veranstaltungsdatum;
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }


    private void FertigeGruppenAusblenden(object sender, RoutedEventArgs e)
    {
        FertigeGruppenAusblendenHelper();
    }

    private void FertigeGruppenAusblendenHelper()
    {
        try
        {
            if (FertigeGruppenAusblenden_Checkbox.IsChecked == true)
            {
                // Filter aktivieren, um Gruppen die alle Felder ausgefüllt haben auszublenden
                var view = CollectionViewSource.GetDefaultView(gruppenListBox.ItemsSource);
                view.Filter = item =>
                {
                    if (item is Jugendfeuerwehr gruppe) return !(gruppe.GezahlterBeitrag >= gruppe.ZuBezahlenderBetrag && gruppe.Einverstaendniserklaerung == true);
                    return true;
                };
            }
            else
            {
                // Filter deaktivieren, um alle Gruppen anzuzeigen
                var view = CollectionViewSource.GetDefaultView(gruppenListBox.ItemsSource);
                view.Filter = null;
            }
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }


    //Fenster Skalieren
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            var viewModel = (MainViewModel)DataContext;

            // Annahme: Mindestgröße für die Skalierung festlegen
            double minWindowSize = 1600; // Minimale Fensterbreite

            // Berechne den Skalierungsfaktor basierend auf der aktuellen Fensterbreite
            var scaleFactor = Math.Min(1, ActualWidth / minWindowSize);

            // Setze den Skalierungsfaktor im ViewModel
            viewModel.ScaleFactor = scaleFactor;
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }

    private void LaunchAnmeldung_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(UrlderAnmeldung.Text) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Webseitenaufruf fehlgeschlagen\n{ex}", "Fehler: Webseitenaufruf", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    private void AnmeldeURLKopieren_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var urlderAnmeldung = UrlderAnmeldung.Text;
            if (urlderAnmeldung != null) Clipboard.SetText(urlderAnmeldung);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }

    private void EmailSchreiben_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var emailVerantwortlicher = EmailVerantwortlicher.Text;
            if (!string.IsNullOrEmpty(emailVerantwortlicher))
            {
                Clipboard.SetText(emailVerantwortlicher);
                var mailto = $"mailto:{emailVerantwortlicher}";
                Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("Keine E-Mail-Adresse für den Verantwortlichen eingetragen.",
                    "Fehler: E-Mail schreiben", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim Öffnen des E-Mail-Clients\n{ex}", "Fehler: E-Mail schreiben",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TelefonnummerKopieren_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var telefonVerantwortlicher = TelefonVerantwortlicher.Text;
            if (!string.IsNullOrEmpty(telefonVerantwortlicher))
            {
                Clipboard.SetText(telefonVerantwortlicher);
                var mailto = $"tel:{telefonVerantwortlicher}";
                Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("Keine E-Mail-Adresse für den Verantwortlichen eingetragen.",
                    "Fehler: E-Mail schreiben", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }

    private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Prüfen, ob die Eingabe ein gültiger Dezimalwert ist
        e.Handled = !IsTextAllowed(e.Text);
    }

    private static bool IsTextAllowed(string text)
    {
        // Verwenden Sie Regex, um nur Zahlen und Dezimaltrennzeichen zuzulassen
        return Regex.IsMatch(text, @"^[0-9]*(?:\.[0-9]*)?$");
    }
}

public class AgeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int age)
            if (age > 18 || age < 10)
                return Brushes.Red; // Farbe Rot für Alter über 18 oder kleiner 10

        return Brushes.Black; // Standardfarbe
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BirthdayToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime birthday)
        {
            var age = Globals.VERANSTALTUNGSDATUM.Year - birthday.Year;
            if (birthday.Date > Globals.VERANSTALTUNGSDATUM.AddYears(-age)) age--;
            // Überprüfen, ob das Geburtsdatum vor dem Veranstaltungsdatum -10 Jahre liegt
            if (age > 18 || age < 10) return Brushes.Red;
        }

        return Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}