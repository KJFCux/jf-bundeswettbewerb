using BWB_Auswertung.IO;
using BWB_Auswertung.Models;
using BWB_Auswertung.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
using Renci.SshNet;

namespace BWB_Auswertung.Views
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly string ProgrammName = System.AppDomain.CurrentDomain.FriendlyName;
        private string settingsPath;
        private string? snapshotXml;
        private bool savedAndClosing = false;
        private readonly ThemePreference initialThemePreference;

        public ThemeService ThemeService => ThemeService.Current;

        public SettingsWindow() : this(new MainViewModel())
        {
        }

        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ProgrammName, "Einstellungen");
            DirectoryInfo di = Directory.CreateDirectory(settingsPath);
            LoadSettings();
            CaptureSnapshot();
            initialThemePreference = ThemeService.Current.Preference;
        }

        private void CaptureSnapshot()
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)DataContext;
                snapshotXml = SerializeXML<Settings>.Serialize(viewModel.Einstellungen);
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                snapshotXml = null;
            }
        }

        private bool HasUnsavedChanges()
        {
            try
            {
                if (snapshotXml == null) return false;
                MainViewModel viewModel = (MainViewModel)DataContext;
                string current = SerializeXML<Settings>.Serialize(viewModel.Einstellungen);
                return !string.Equals(current, snapshotXml, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private bool SaveSettings()
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)DataContext;
                WriteFile.writeText(System.IO.Path.Combine(settingsPath, "settings.xml"), SerializeXML<Settings>.Serialize(viewModel.Einstellungen));
                CaptureSnapshot();
                return true;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim speichern von Einstellungen\n{ex}", "Fehler: Einstellungen", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            if (SaveSettings())
            {
                ThemeService.Current.Persist();
                savedAndClosing = true;
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool themeChanged = ThemeService.Current.Preference != initialThemePreference;

            if (savedAndClosing) return;

            if (!HasUnsavedChanges() && !themeChanged) return;

            MessageBoxResult result = MessageBox.Show(
                "Es gibt ungespeicherte Änderungen. Sollen diese gespeichert werden?",
                "Ungespeicherte Änderungen",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (!SaveSettings())
                {
                    e.Cancel = true;
                }
                else
                {
                    ThemeService.Current.Persist();
                }
            }
            else if (result == MessageBoxResult.No)
            {
                if (themeChanged)
                {
                    ThemeService.Current.Preference = initialThemePreference;
                }
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        private void StartzeitenLoeschen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)DataContext;
                if (viewModel.Gruppen == null || viewModel.Gruppen.Count == 0)
                {
                    MessageBox.Show("Es sind keine Gruppen vorhanden.",
                        "Alle Startzeiten löschen", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    $"Sollen wirklich bei allen {viewModel.Gruppen.Count} Gruppen die Startzeiten und Bahnen für A- und B-Teil gelöscht werden?\n\nDieser Vorgang kann nicht rückgängig gemacht werden.",
                    "Alle Startzeiten löschen",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.OK) return;

                foreach (var gruppe in viewModel.Gruppen)
                {
                    gruppe.StartzeitATeil = default;
                    gruppe.StartzeitBTeil = default;
                    gruppe.WettbewerbsbahnATeil = null;
                    gruppe.WettbewerbsbahnBTeil = null;
                }

                MessageBox.Show($"Startzeiten und Bahnen von {viewModel.Gruppen.Count} Gruppen wurden gelöscht.",
                    "Alle Startzeiten löschen", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim Löschen der Startzeiten\n{ex}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoStartzeiten_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainViewModel viewModel = (MainViewModel)DataContext;
                if (viewModel.Gruppen == null || viewModel.Gruppen.Count == 0)
                {
                    MessageBox.Show("Es sind keine Gruppen vorhanden, denen Startzeiten zugewiesen werden könnten.",
                        "Automatische Startzeitenvergabe", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                AutoStartReihenfolge reihenfolge = ReihenfolgeComboBox.SelectedValue is AutoStartReihenfolge sel
                    ? sel
                    : AutoStartReihenfolge.AktuelleSortierung;

                var result = StartzeitenService.AutoAssign(viewModel.Gruppen, viewModel.Einstellungen, reihenfolge);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"A-Teil: {result.ZugewieseneA} Startzeit(en) vergeben.");
                sb.AppendLine($"B-Teil: {result.ZugewieseneB} Startzeit(en) vergeben.");
                if (result.Warnungen.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Warnungen:");
                    foreach (var w in result.Warnungen)
                    {
                        sb.AppendLine($" • {w}");
                    }
                }

                MessageBox.Show(sb.ToString(), "Automatische Startzeitenvergabe",
                    MessageBoxButton.OK,
                    result.Warnungen.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Fehler bei der automatischen Startzeitenvergabe\n{ex}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void TesteVerbindung_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var viewModel = (MainViewModel)DataContext;
                var einstellungen = viewModel.Einstellungen;

                using (var sftp = SftpFactory.Create(einstellungen))
                {
                    sftp.Connect();
                    if (sftp.IsConnected)
                        MessageBox.Show("Verbindung erfolgreich!", "Erfolg", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    else
                        MessageBox.Show("Verbindung fehlgeschlagen!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    sftp.Disconnect();
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name,
                    EventLogEntryType.Error);
                MessageBox.Show($"Fehler beim Verbinden mit SFTP\n{ex}", "Fehler: Einstellungen",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Fenster Skalieren
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                var viewModel = (MainViewModel)DataContext;

                // Annahme: Mindestgröße für die Skalierung festlegen
                double minWindowSize = 1020; // Minimale Fensterbreite

                // Berechne den Skalierungsfaktor basierend auf der aktuellen Fensterbreite
                var scaleFactor = Math.Min(1, ActualWidth / minWindowSize);

                // Setze den Skalierungsfaktor im ViewModel
                viewModel.ScaleFactorSettings = scaleFactor;
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

        private bool _suppressPasswordSync;

        private void SftpPasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox == null) return;

            var viewModel = DataContext as MainViewModel;
            if (viewModel?.Einstellungen == null) return;

            // Gespeichertes Passwort in die PasswordBox zurückschreiben, ohne
            // dabei den PasswordChanged-Handler das ViewModel überschreiben zu lassen.
            _suppressPasswordSync = true;
            try
            {
                passwordBox.Password = viewModel.Einstellungen.Password ?? string.Empty;
            }
            finally
            {
                _suppressPasswordSync = false;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_suppressPasswordSync) return;

            var passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                var viewModel = (MainViewModel)DataContext;
                viewModel.Einstellungen.Password = passwordBox.Password;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}
