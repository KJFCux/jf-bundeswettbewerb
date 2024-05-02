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
using Squirrel;

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

            //Speicherort für Logdateien und evtl. Ordner erstellen
            _ = Directory.CreateDirectory(System.IO.Path.Combine(AppDataLocal, ProgrammName, "log"));

            //Beim Starten des Programms evtl. Bereits vorhandene Gruppen wieder in das Programm laden
            LoadData();

            //Einstellungen Laden
            settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ProgrammName, "Einstellungen");
            _ = Directory.CreateDirectory(settingsPath);
            LoadSettings();
            _ = CheckForUpdates();
        }
        private async Task CheckForUpdates()
        {
            using (var manager = new UpdateManager(BWB_Auswertung.Properties.Settings.Default.GithubURL))
            {
                await manager.UpdateApp();
            }
        }

        private void LaunchWebsite_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("http://www.kjf-cux.de") { UseShellExecute = true });
        }

        private void Github_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo(BWB_Auswertung.Properties.Settings.Default.GithubURL) { UseShellExecute = true });
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO");
        }

        private void Import_Click(object sender, RoutedEventArgs e)
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
            viewModel.Sort(sortComboBox.SelectedIndex);

        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            //Speichern an dem in der DialogBox angegebenen Ort
            SaveData(new Dialogs().ShowFolderBrowserDialog(), false);
        }

        private void OpenEvaluation_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel viewModel = (MainViewModel)this.DataContext;
            if (viewModel.Gruppen.Count == 0)
            {
                MessageBox.Show("Keine Gruppen vorhanden", "Auswertung kann nicht geöffnet werden", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //Vor dem öffnen der Auswertung die Liste neu sortieren um evtl.
            //Platz Änderungen korrekt zu setzen
            viewModel.Sort(sortComboBox.SelectedIndex);
            EvaluationView neuesFenster = new EvaluationView(viewModel);
            neuesFenster.ShowDialog();
        }

        private void Hilfe_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO");
        }

        private void version_Click(object sender, RoutedEventArgs e)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string message = $"{ProgrammName} v.{versionInfo.FileVersion}";
            MessageBox.Show(message, "Version", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void gruppenListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        }
        private void sortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }


        //Hinzufügen einer leeren neuen Gruppe
        private void ButtonAddGroup_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel viewModel = (MainViewModel)this.DataContext;
            viewModel.AddEmptyGruppe("", "Neue Gruppe");
        }

        //Anzeigen der globalen Einstellungen
        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow neuesFenster = new SettingsWindow();
            bool? result = neuesFenster.ShowDialog();
            LoadSettings();
        }


        //Beim schließen des Programms alle Daten speichern
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveData(dataPath, true);
        }

        //Auf drücken von Strg+S alles speichern
        private void CommandSaving(object sender, ExecutedRoutedEventArgs e)
        {
            SaveData(dataPath, true);
        }

        private void CommandSaving_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        //Speichert alle Daten als einzelne XML Dateien
        private void SaveData(string savePath, bool deleteOld = false)
        {
            List<string> aktuelleDateien = new List<string>();
            MainViewModel viewModel = (MainViewModel)this.DataContext;
            foreach (var gruppe in viewModel.Gruppen)
            {
                string datei = System.IO.Path.Combine($"{gruppe.Feuerwehr} - {gruppe.GruppenName}.xml");
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

        //Dateien aus dem Ordner Ordner laden
        private void LoadData()
        {
            string[] xmlFiles = Directory.GetFiles(dataPath, "*.xml");
            foreach (string file in xmlFiles)
            {
                OpenDeserializerForFile(file);
            }
        }

        private void OpenDeserializerForFile(string file, bool ueberrschreiben = false)
        {
            MainViewModel viewModel = (MainViewModel)this.DataContext;

            // Deserialisieren der XML-Datei und Hinzufügen der deserialisierten Gruppen zum ViewModel
            Gruppe gruppe = DeserializeXML<Gruppe>.Deserialize<Gruppe>(file);
            if (gruppe != null)
            {
                //Schauen ob bereits vorhanden und alten Eintrag löschen
                if (ueberrschreiben)
                {
                    var gefundeneGruppen = viewModel.Gruppen.Where(x => x.GruppenName.Equals(gruppe.GruppenName)).ToList();

                    foreach (var gefundeneGruppe in gefundeneGruppen)
                    {
                        //Die Startzeiten/Bahnen sollen nicht überschrieben werden, sofern sie im Import nicht gesetzt sind
                        if (gruppe.StartzeitATeil == DateTime.MinValue) gruppe.StartzeitATeil = gefundeneGruppe.StartzeitATeil;
                        if (gruppe.StartzeitBTeil == DateTime.MinValue) gruppe.StartzeitBTeil = gefundeneGruppe.StartzeitBTeil;
                        if (gruppe.WettbewerbsbahnATeil == null) gruppe.WettbewerbsbahnATeil = gefundeneGruppe.WettbewerbsbahnATeil;
                        if (gruppe.WettbewerbsbahnBTeil == null) gruppe.WettbewerbsbahnBTeil = gefundeneGruppe.WettbewerbsbahnBTeil;

                        // Alte Gruppe löschen
                        viewModel.RemoveSelectedGroup(gefundeneGruppe, false);
                    }


                }
                //Neuen Eintrag importieren
                viewModel.AddGroup(gruppe);
            }
        }

        private void LoadSettings()
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

        private void ersatzComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Stellen Sie sicher, dass ein Element ausgewählt ist und es sich um eine Gruppe handelt
            if (gruppenListBox != null)
            {
                if (ersatzComboBox.SelectedIndex >= 1 && gruppenListBox.SelectedItem != null && gruppenListBox.SelectedItem is Gruppe selectedGruppe)
                {

                    MainViewModel viewModel = (MainViewModel)this.DataContext;
                    viewModel.switchBoxTeilnehmer(ersatzComboBox.SelectedIndex, selectedGruppe);
                    ersatzComboBox.SelectedIndex = 0;
                }
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
                viewModel.Sort(sortComboBox.SelectedIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }

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