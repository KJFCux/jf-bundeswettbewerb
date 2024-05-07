using BWB_Auswertung.IO;
using BWB_Auswertung.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BWB_Auswertung.Views
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly string ProgrammName = System.AppDomain.CurrentDomain.FriendlyName;
        private string settingsPath;

        public SettingsWindow()
        {

            InitializeComponent();
            DataContext = new MainViewModel();

            settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ProgrammName, "Einstellungen");
            DirectoryInfo di = Directory.CreateDirectory(settingsPath);
            LoadSettings();
        }

        private void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                WriteFile.writeText(System.IO.Path.Combine(settingsPath, "settings.xml"), SerializeXML<Gruppe>.Serialize(viewModel.Einstellungen));
                this.Close();
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim speichern von Einstellungen\n{ex}", "Fehler: Einstellungen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim laden der Einstellungen\n{ex}", "Fehler: Einstellungen", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }


        private void SelectLogoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Erstellen und Konfigurieren des OpenFileDialog
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Bilddateien (*.jpg; *.jpeg; *.png; *.gif)|*.jpg; *.jpeg; *.png; *.gif|Alle Dateien (*.*)|*.*";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                openFileDialog.Title = "Logo auswählen";

                // Öffnen des Dialogs und Überprüfen, ob der Benutzer eine Datei ausgewählt hat
                if (openFileDialog.ShowDialog() == true)
                {
                    // Der ausgewählte Dateipfad wird in der TextBox angezeigt
                    SelectedLogoPathTextBox.Text = openFileDialog.FileName;
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim Festlegen von Einstellungen\n{ex}", "Fehler: Einstellungen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectUnterschriftrechtsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Erstellen und Konfigurieren des OpenFileDialog
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Bilddateien (*.jpg; *.jpeg; *.png; *.gif)|*.jpg; *.jpeg; *.png; *.gif|Alle Dateien (*.*)|*.*";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                openFileDialog.Title = "Unterschrift 2 auswählen";

                // Öffnen des Dialogs und Überprüfen, ob der Benutzer eine Datei ausgewählt hat
                if (openFileDialog.ShowDialog() == true)
                {
                    // Der ausgewählte Dateipfad wird in der TextBox angezeigt
                    SelectedUnterschriftrechtsPathTextBox.Text = openFileDialog.FileName;
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim Festlegen von Einstellungen\n{ex}", "Fehler: Einstellungen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectUnterschriftlinksButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Erstellen und Konfigurieren des OpenFileDialog
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Bilddateien (*.jpg; *.jpeg; *.png; *.gif)|*.jpg; *.jpeg; *.png; *.gif|Alle Dateien (*.*)|*.*";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                openFileDialog.Title = "Unterschrift 1 auswählen";

                // Öffnen des Dialogs und Überprüfen, ob der Benutzer eine Datei ausgewählt hat
                if (openFileDialog.ShowDialog() == true)
                {
                    // Der ausgewählte Dateipfad wird in der TextBox angezeigt
                    SelectedUnterschriftlinksPathTextBox.Text = openFileDialog.FileName;
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim Festlegen von Einstellungen\n{ex}", "Fehler: Einstellungen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
