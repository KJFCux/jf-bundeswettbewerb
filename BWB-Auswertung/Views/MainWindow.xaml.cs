﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using BWB_Auswertung.Models;
using System.Globalization;
using System.Threading;
using System.Windows.Markup;
using BWB_Auswertung.IO;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using BWB_Auswertung.Views;
using System.Text.Json;
using System.Net.Http;
using ControlzEx.Standard;
using System.Text.RegularExpressions;
using System.Reflection;
using Renci.SshNet;
using System.Web;

namespace BWB_Auswertung
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly string AppDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private readonly string ProgrammName = System.AppDomain.CurrentDomain.FriendlyName;
        private string dataPath;
        private string settingsPath;
        private string currentVersion = "";

        public MainWindow()
        {

            //Alles umstellen auf Deutsche Lokalisierung
            var vCulture = new CultureInfo("de-DE");

            Thread.CurrentThread.CurrentCulture = vCulture;
            Thread.CurrentThread.CurrentUICulture = vCulture;
            CultureInfo.DefaultThreadCurrentCulture = vCulture;
            CultureInfo.DefaultThreadCurrentUICulture = vCulture;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(
            XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));


            //Initialize
            InitializeComponent();
            DataContext = new MainViewModel();

            //Speicherort für alle Gruppendaten festlegen und evtl. Ordner erstellen
            dataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ProgrammName, "Gruppendaten");
            _ = Directory.CreateDirectory(dataPath);

            LOGGING.Write(dataPath, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);


            //Speicherort für Logdateien und evtl. Ordner erstellen
            _ = Directory.CreateDirectory(System.IO.Path.Combine(AppDataLocal, ProgrammName, "log"));

            //Beim Starten des Programms evtl. Bereits vorhandene Gruppen wieder in das Programm laden
            LoadData();

            //Einstellungen Laden
            settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ProgrammName, "Einstellungen");
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
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.Version versionInfo = assembly.GetName().Version;
                currentVersion = $"v{versionInfo.Major}.{versionInfo.MajorRevision}.{versionInfo.Build}";

                // HTTP Client erstellen und den aktuellen ReleaseTag von Github abfragen
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "UpdateChecker");

                    HttpResponseMessage response = await client.GetAsync(BWB_Auswertung.Properties.Settings.Default.GithubReleaseURL);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();

                    List<GitHub> releases = JsonSerializer.Deserialize<List<GitHub>>(responseBody);

                    // Letzten release ermitteln
                    GitHub latestRelease = releases[0];

                    // Mit lokaler Version vergleichen
                    if (latestRelease.tag_name.CompareTo($"BWB-Auswertung/{currentVersion}") > 0)
                    {
                        // Eine neue Version ist verfügbar. Fragen ob die Datei heruntergeladen werden soll
                        var result = MessageBox.Show($"Es gibt eine neue Version des Auswertungsprogramms!\nMöchtest du die neue Version herunterladen?", "Update verfügbar!", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo(BWB_Auswertung.Properties.Settings.Default.GithubDownloadURL.Replace("{release}", latestRelease.tag_name)) { UseShellExecute = true });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }

            return false;
        }

        private void LaunchWebsite_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(BWB_Auswertung.Properties.Settings.Default.BWBURL) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Webseitenaufruf fehlgeschlagen\n{ex}", "Fehler: Webseitenaufruf", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Github_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(BWB_Auswertung.Properties.Settings.Default.GithubURL) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Webseitenaufruf fehlgeschlagen\n{ex}", "Fehler: Webseitenaufruf", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(BWB_Auswertung.Properties.Settings.Default.BWBURL) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Webseitenaufruf fehlgeschlagen\n{ex}", "Fehler: Webseitenaufruf", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Hilfe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(BWB_Auswertung.Properties.Settings.Default.BWBURL) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Webseitenaufruf fehlgeschlagen\n{ex}", "Fehler: Webseitenaufruf", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "JF-BWB Dateien (*.XML)|*.xml|All files (*.*)|*.*";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (openFileDialog.ShowDialog() == true)
                {
                    foreach (string file in openFileDialog.FileNames)
                    {
                        OpenDeserializerForFile(file, true);
                    }
                }
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                //Filterung entfernen
                FertigeGruppenAusblenden_Checkbox.IsChecked = false;

                //Importiertes einsortieren
                viewModel.Sort(sortComboBox.SelectedIndex);
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Import fehlgeschlagen\n{ex}", "Fehler: Import", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Speichern an dem in der DialogBox angegebenen Ort
                SaveData(new Dialogs().ShowFolderBrowserDialog(), false);
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export fehlgeschlagen\n{ex}", "Fehler: Export", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenEvaluation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                if (viewModel.Gruppen.Count == 0)
                {
                    MessageBox.Show("Keine Gruppen vorhanden", "Auswertung kann nicht geöffnet werden", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                //Filterung entfernen
                FertigeGruppenAusblenden_Checkbox.IsChecked = false;
                //Vor dem öffnen der Auswertung die Liste neu sortieren um evtl.
                //Platz Änderungen korrekt zu setzen
                viewModel.Sort(sortComboBox.SelectedIndex);
                EvaluationView neuesFenster = new EvaluationView(viewModel);
                neuesFenster.ShowDialog();
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Öffen der Auswertung fehlgeschlagen\n{ex}", "Fehler: Öffne Auswertung", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void version_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string message = $"{ProgrammName} {currentVersion}";
                MessageBox.Show(message, "Version", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Ermitteln der Version fehlgeschlagen\n{ex}", "Fehler: Version", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void gruppenListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Stellen Sie sicher, dass ein Element ausgewählt ist und es sich um eine Gruppe handelt
                if (gruppenListBox.SelectedItem != null && gruppenListBox.SelectedItem is Gruppe selectedGruppe)
                {
                    // Der DataContext wird automatisch aktualisiert
                }
                //Wenn die Sortierung einen Wert hat, danach sortieren

                //Alles neu sortieren
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.Sort(sortComboBox.SelectedIndex);

                //Beim Durchklicken durch die Gruppen alle Daten speichern
                //Beim Durchklicken sind evtl. Bearbeitungen abgeschlossen. Daher alles speichern
                SaveData(dataPath, true);
                FertigeGruppenAusblendenHelper();
                gruppenListBox.Focus();
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler bei Gruppenauswahl\n{ex}", "Fehler: Gruppenauswahl", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void sortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Stellen Sie sicher, dass ein Element ausgewählt ist und es sich um eine Gruppe handelt
                if (gruppenListBox != null)
                {
                    if (gruppenListBox.SelectedItem != null && gruppenListBox.SelectedItem is Gruppe selectedGruppe)
                    {
                        // Der DataContext wird automatisch aktualisiert
                    }
                    MainViewModel viewModel = (MainViewModel)this.DataContext;
                    viewModel.Sort(sortComboBox.SelectedIndex);
                    FertigeGruppenAusblendenHelper();
                    gruppenListBox.Focus();
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim sortieren der Gruppen\n{ex}", "Fehler: Gruppensortierung", MessageBoxButton.OK, MessageBoxImage.Error);
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
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim durschalten der Gruppen\n{ex}", "Fehler: Gruppen", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                viewModel.AddEmptyGruppe("", "Neue Gruppe");
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim hinzufügen einer neuen Gruppe\n{ex}", "Fehler: Neue Gruppe", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Anzeigen der globalen Einstellungen
        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SettingsWindow neuesFenster = new SettingsWindow();
                bool? result = neuesFenster.ShowDialog();
                LoadSettings();
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim öffnen der Einstellungen\n{ex}", "Fehler: Öffne Einstellungen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "Excel Meldebögen|*.xlsx;";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (openFileDialog.ShowDialog() == true)
                {
                    foreach (string file in openFileDialog.FileNames)
                    {

                        var gruppe = Excel.ImportExcelGruppe(file);
                        if (gruppe != null)
                        {

                            //Alte überschreiben
                            var gefundeneGruppen = viewModel.Gruppen.Where(x => x.GruppenName.Equals(gruppe.GruppenName)).ToList();

                            foreach (var gefundeneGruppe in gefundeneGruppen)
                            {
                                viewModel.RemoveSelectedGroup(gefundeneGruppe, false);
                            }
                            viewModel.AddGroup(gruppe);
                        }


                    }
                }

                //Filterung entfernen
                FertigeGruppenAusblenden_Checkbox.IsChecked = false;

                //Hinzugefügtes einsortieren
                viewModel.Sort(sortComboBox.SelectedIndex);

            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim Excel Import\n{ex}", "Fehler: Import", MessageBoxButton.OK, MessageBoxImage.Error);
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
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim speichern\n{ex}", "Fehler: Speichern", MessageBoxButton.OK, MessageBoxImage.Error);
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
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim speichern\n{ex}", "Fehler: Speichern", MessageBoxButton.OK, MessageBoxImage.Error);
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
                List<string> aktuelleDateien = new List<string>();
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                foreach (var gruppe in viewModel.Gruppen)
                {
                    string datei = System.IO.Path.Combine($"{gruppe.FeuerwehrOhneSonderzeichen} - {gruppe.GruppennameOhneSonderzeichen}.xml");
                    WriteFile.writeText(System.IO.Path.Combine(savePath, datei), SerializeXML<Gruppe>.Serialize(gruppe));

                    //Dateinamen merken um alte löschen zu können
                    aktuelleDateien.Add(datei);
                }

                // Nicht mehr benötigte Dateien löschen
                if (deleteOld)
                {
                    DeleteFiles.DeleteFilesExcept(aktuelleDateien, savePath);
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        //Dateien aus dem Ordner Ordner laden
        private void LoadData()
        {
            try
            {
                string[] xmlFiles = Directory.GetFiles(dataPath, "*.xml");
                foreach (string file in xmlFiles)
                {
                    OpenDeserializerForFile(file);
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private void OpenDeserializerForFile(string file, bool ueberrschreiben = false, bool showOverrideInfo = true)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                // Deserialisieren der XML-Datei und Hinzufügen der deserialisierten Gruppen zum ViewModel
                Gruppe gruppe = DeserializeXML<Gruppe>.Deserialize<Gruppe>(file);
                if (gruppe == null)
                {
                    LOGGING.Write("Die Datei konnte nicht korrekt geparst werden. Überprüfen Sie die Struktur und den Inhalt der XML-Datei.", MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
                    MessageBox.Show($"Die Datei konnte nicht korrekt geparst werden. Überprüfen Sie die Struktur und den Inhalt der XML-Datei.", "Fehler: SFTP Sync", MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    throw new InvalidDataException("Die Datei konnte nicht korrekt geparst werden. Überprüfen Sie die Struktur und den Inhalt der XML-Datei.");
                }


                //Schauen ob bereits vorhanden und alten Eintrag löschen
                if (ueberrschreiben)
                {
                    var gefundeneGruppen = viewModel.Gruppen.Where(x => x.GruppenName.Equals(gruppe.GruppenName)).ToList();

                    foreach (var gefundeneGruppe in gefundeneGruppen)
                    {
                        // Verhindern, dass vorhandene Bewertungen oder Startzeiten überschrieben werden.
                        // Nur die Personen und Namen werden aus dem Import übernommen

                        Gruppe gruppeTemp = gefundeneGruppe;
                        gruppeTemp.Persons = gruppe.Persons;
                        gruppeTemp.Feuerwehr = gruppe.Feuerwehr;
                        gruppeTemp.GruppenName = gruppe.GruppenName;
                        gruppeTemp.Organisationseinheit = gruppe.Organisationseinheit;

                        gruppe = gruppeTemp;

                        //Hinweis an Benutzer das die Gruppe existiert
                        if (showOverrideInfo)
                            MessageBox.Show($"Die Gruppe {gruppe.GruppenName} von der Ortswehr {gruppe.Feuerwehr} aus {gruppe.Organisationseinheit} Existierte bereits und wurde überschrieben!\nURL der Anmeldung neu: {gruppe.UrlderAnmeldung}\nURL der Anmeldung alt: {gefundeneGruppe.UrlderAnmeldung}", "Gruppe wurde überschrieben!", MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Alte Gruppe löschen
                        viewModel.RemoveSelectedGroup(gefundeneGruppe, false);
                    }


                }
                //Neuen Eintrag importieren
                viewModel.AddGroup(gruppe);

            }
            catch (Exception ex)
            {
                if (ex is InvalidDataException) throw;
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private void SyncmitFtp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // SFTP Einstellungen aus dem Model laden
                var viewModel = (MainViewModel)DataContext;
                var einstellungen = viewModel.Einstellungen;
                if (!TesteVerbindung_Click())
                {
                    MessageBox.Show("Bitte prüfe die FTP Einstellungen", "Verbindung fehlgeschlagen!",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

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
                    WriteFile.writeText(Path.Combine(uploadlocalPath, datei), SerializeXML<Gruppe>.Serialize(gruppe));
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
        private bool TesteVerbindung_Click()
        {
            try
            {
                var viewModel = (MainViewModel)DataContext;
                var einstellungen = viewModel.Einstellungen;

                using (var sftp = new SftpClient(einstellungen.Hostname, 22, einstellungen.Username,
                           einstellungen.Password))
                {
                    sftp.Connect();
                    if (sftp.IsConnected)
                    {
                        sftp.Disconnect();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name,
                    EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim Verbinden mit SFTP\n{ex}", "Fehler: Einstellungen",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;

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
                if (xmlFile.Length < 1)
                {
                    return;
                }


                MainViewModel viewModel = (MainViewModel)this.DataContext;

                // Deserialisieren der XML-Datei und Hinzufügen der deserialisierten Gruppen zum ViewModel
                Settings einstellungen = DeserializeXML<Settings>.Deserialize<Settings>(xmlFile[0]);
                if (einstellungen != null)
                {
                    viewModel.OverrideSettings(einstellungen);
                }

                //Veranstaltungszeit Global setzen
                Globals.SECONDS_ATEIL = viewModel.Einstellungen.Vorgabezeit;
                Globals.VERANSTALTUNGSDATUM = viewModel.Einstellungen.Veranstaltungsdatum;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private void ersatzComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Stellen Sie sicher, dass ein Element ausgewählt ist und es sich um eine Gruppe handelt
                if (gruppenListBox != null)
                {
                    if (ersatzComboBox.SelectedIndex >= 1 && gruppenListBox.SelectedItem != null && gruppenListBox.SelectedItem is Gruppe selectedGruppe)
                    {

                        MainViewModel viewModel = (MainViewModel)this.DataContext;
                        viewModel.switchBoxTeilnehmer(ersatzComboBox.SelectedIndex, selectedGruppe);
                        ersatzComboBox.SelectedIndex = 0;
                        FertigeGruppenAusblendenHelper();
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
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
                    ICollectionView view = CollectionViewSource.GetDefaultView(gruppenListBox.ItemsSource);
                    view.Filter = item =>
                    {
                        if (item is Gruppe gruppe)
                        {
                            return ((gruppe.ATeilGesamteindruck < 1)
                                     || (gruppe.BTeilGesamteindruck < 1)
                                     || (gruppe.PunkteBTeil < 1)
                                     || (gruppe.DurchschnittszeitKnotenATeil < 1)
                                     || (gruppe.DurchschnittszeitBTeil < 1)
                                     || (gruppe.DurchschnittszeitATeil < 1)
                                     );
                        }
                        return false;
                    };
                }
                else
                {
                    // Filter deaktivieren, um alle Gruppen anzuzeigen
                    ICollectionView view = CollectionViewSource.GetDefaultView(gruppenListBox.ItemsSource);
                    view.Filter = null;
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }


        //Fenster Skalieren
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                // Annahme: Mindestgröße für die Skalierung festlegen
                double minWindowSize = 1600; // Minimale Fensterbreite

                // Berechne den Skalierungsfaktor basierend auf der aktuellen Fensterbreite
                double scaleFactor = Math.Min(1, ActualWidth / minWindowSize);

                // Setze den Skalierungsfaktor im ViewModel
                viewModel.ScaleFactor = scaleFactor;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private void AnmeldeURLKopieren_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? urlderAnmeldung = UrlderAnmeldung.Text?.ToString();
                if (urlderAnmeldung != null) Clipboard.SetText(urlderAnmeldung);

            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
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

        private bool _focusedByPointer;

        private void TextBox_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            _focusedByPointer = true;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!_focusedByPointer)
            {
                (sender as TextBox)?.SelectAll();
            }
            _focusedByPointer = false;
        }

    }

    public class AgeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int age)
            {
                if (age > 18 || age < 10)
                {
                    return Brushes.Red; // Farbe Rot für Alter über 18 oder kleiner 10
                }
            }
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
                if (age > 18 || age < 10)
                {
                    return Brushes.Red;
                }
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
