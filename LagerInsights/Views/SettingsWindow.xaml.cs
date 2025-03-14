using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LagerInsights.IO;
using LagerInsights.Models;
using Microsoft.Win32;
using Renci.SshNet;

namespace LagerInsights.Views;

/// <summary>
///     Interaktionslogik für SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly string ProgrammName = AppDomain.CurrentDomain.FriendlyName;
    private readonly string settingsPath;

    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();

        settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            ProgrammName, "Einstellungen");
        var di = Directory.CreateDirectory(settingsPath);
        LoadSettings();
    }

    private void SaveAndClose_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var viewModel = (MainViewModel)DataContext;
            WriteFile.writeText(Path.Combine(settingsPath, "settings.xml"),
                SerializeXML<Jugendfeuerwehr>.Serialize(viewModel.Einstellungen));
            Close();
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name,
                EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim speichern von Einstellungen\n{ex}", "Fehler: Einstellungen",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            var einstellungen = DeserializeXML<Settings>.Deserialize<Settings>(xmlFile[0]);
            if (einstellungen != null) viewModel.OverrideSettings(einstellungen);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name,
                EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim laden der Einstellungen\n{ex}", "Fehler: Einstellungen",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void SelectLogoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Erstellen und Konfigurieren des OpenFileDialog
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter =
                "Bilddateien (*.jpg; *.jpeg; *.png; *.gif)|*.jpg; *.jpeg; *.png; *.gif|Alle Dateien (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            openFileDialog.Title = "Logo auswählen";

            // Öffnen des Dialogs und Überprüfen, ob der Benutzer eine Datei ausgewählt hat
            if (openFileDialog.ShowDialog() == true)
                // Der ausgewählte Dateipfad wird in der TextBox angezeigt
                SelectedLogoPathTextBox.Text = openFileDialog.FileName;
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name,
                EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim Festlegen von Einstellungen\n{ex}", "Fehler: Einstellungen",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SelectUnterschriftrechtsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Erstellen und Konfigurieren des OpenFileDialog
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter =
                "Bilddateien (*.jpg; *.jpeg; *.png; *.gif)|*.jpg; *.jpeg; *.png; *.gif|Alle Dateien (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            openFileDialog.Title = "Unterschrift 2 auswählen";

            // Öffnen des Dialogs und Überprüfen, ob der Benutzer eine Datei ausgewählt hat
            if (openFileDialog.ShowDialog() == true)
                // Der ausgewählte Dateipfad wird in der TextBox angezeigt
                SelectedUnterschriftrechtsPathTextBox.Text = openFileDialog.FileName;
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name,
                EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim Festlegen von Einstellungen\n{ex}", "Fehler: Einstellungen",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SelectUnterschriftlinksButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Erstellen und Konfigurieren des OpenFileDialog
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter =
                "Bilddateien (*.jpg; *.jpeg; *.png; *.gif)|*.jpg; *.jpeg; *.png; *.gif|Alle Dateien (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            openFileDialog.Title = "Unterschrift 1 auswählen";

            // Öffnen des Dialogs und Überprüfen, ob der Benutzer eine Datei ausgewählt hat
            if (openFileDialog.ShowDialog() == true)
                // Der ausgewählte Dateipfad wird in der TextBox angezeigt
                SelectedUnterschriftlinksPathTextBox.Text = openFileDialog.FileName;
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name,
                EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim Festlegen von Einstellungen\n{ex}", "Fehler: Einstellungen",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void ButtonAddZeltdorf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var viewModel = (MainViewModel)DataContext;

            string neuesZeltdorf = TextBoxZeltdorf.Text;
            if (!viewModel.Zeltdoerfer.Contains(neuesZeltdorf))
            {
                // Füge das Element in der ListBox und den Einstellungen hinzu
                viewModel.Zeltdoerfer.Add(neuesZeltdorf);
                viewModel.Einstellungen.Zeltdoerfer.Add(neuesZeltdorf);
            }

        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim hinzufügen eines Zeltdorfes\n{ex}", "Fehler: Neues Zeltdorf",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ButtonRemoveZeltdorf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var viewModel = (MainViewModel)DataContext;

            var selectedZeltdoerfer = ZeltdorfListBox.SelectedItems.Cast<string>().ToList();
            foreach (var zeltdorf in selectedZeltdoerfer)
            {
                // Entferne das Element aus der ListBox und den Einstellungen
                viewModel.Zeltdoerfer.Remove(zeltdorf);
                viewModel.Einstellungen.Zeltdoerfer.Remove(zeltdorf);
            }
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            MessageBox.Show($"Fehler beim löschen eines Zeltdorfes\n{ex}", "Fehler: Lösche Zeltdorf",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void TesteVerbindung_Click(object sender, RoutedEventArgs e)
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

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
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