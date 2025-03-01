using LagerInsights.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using LagerInsights.IO;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LagerInsights.Views
{
    /// <summary>
    /// Auswertung der BWB Daten und Exportieren der Listen usw.
    /// </summary>
    public partial class EvaluationView : Window
    {
        private string exportPath = "";
        private string vorlagenPath = "";
        private string wertungsbogenPath = "";

        public EvaluationView(MainViewModel mainViewModel)
        {
            try
            {
                InitializeComponent();
                DataContext = mainViewModel;

                //Setzen der Statusbar auf aktuellen Prozentwert der Anzahl an fertigen Gruppen
                double grundwert = mainViewModel.Gruppen.Count();
                try
                {
                    double prozentwert = mainViewModel.Gruppen.Where(x =>
                    x.GezahlterBeitrag >= x.ZuBezahlenderBetrag).ToList().Count();
                    double prozentsatz = prozentwert / grundwert * 100d;
                    Bezahlfortschritt.Value = prozentsatz;
                    AnzahlFehlenderGruppen.Content = grundwert - prozentwert;
                    GesamtanzahlGruppen.Content = grundwert;
                }
                catch (Exception ex)
                {
                    //Noch keine Daten zum auswerten vorhanden
                    Bezahlfortschritt.Value = 0;
                    AnzahlFehlenderGruppen.Content = grundwert;
                    GesamtanzahlGruppen.Content = grundwert;
                    LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Warning);
                }
                try
                {

                    //Gesamtzahl an Personen
                    var gesamtzahlTeilnehmende = mainViewModel.Gruppen.Sum(g => g.Persons.Count);
                    AnzahlTeilnehmende.Content = gesamtzahlTeilnehmende;

                    //Anzahl W Person
                    var anzahlWTeilnehmer = mainViewModel.Gruppen.Sum(g => g.Persons.Count(p => p.Geschlecht == Gender.W));
                    AnzahlTeilnehmende.Content = anzahlWTeilnehmer;

                    //Anzahl M Person
                    var anzahlMTeilnehmer = mainViewModel.Gruppen.Sum(g => g.Persons.Count(p => p.Geschlecht == Gender.M));
                    AnzahlMaennlich.Content = anzahlMTeilnehmer;

                    //Anzahl D Person
                    var anzahlDTeilnehmer = mainViewModel.Gruppen.Sum(g => g.Persons.Count(p => p.Geschlecht == Gender.D));
                    AnzahlDivers.Content = anzahlDTeilnehmer;

                    //Anzahl N Person
                    var anzahlNTeilnehmer = mainViewModel.Gruppen.Sum(g => g.Persons.Count(p => p.Geschlecht == Gender.N));
                    AnzahlSonstige.Content = anzahlNTeilnehmer;



                    //Gesamt Betrag zu bezahlen
                    var gesamtBetragZuBezahlen = mainViewModel.Gruppen.Sum(g => g.ZuBezahlenderBetrag);
                    GesamtBetragZuBezahlen.Content = gesamtBetragZuBezahlen.ToString("C", CultureInfo.CurrentCulture);

                    //Bereits bezahlter Betrag
                    var gesamtBetragBezahlt = mainViewModel.Gruppen.Sum(g => g.GezahlterBeitrag ?? 0);
                    GesamtBetragBezahlt.Content = gesamtBetragBezahlt.ToString("C", CultureInfo.CurrentCulture);

                    //Offener Betrag
                    var offenerBetrag = gesamtBetragZuBezahlen - gesamtBetragBezahlt;
                    OffenerBetrag.Content = offenerBetrag.ToString("C", CultureInfo.CurrentCulture);
                }
                catch (Exception ex)
                {
                    //Noch keine Daten zum Auswerten vorhanden
                    LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Warning);
                }

                //Speicherort für Exporte festlegen und evtl. Ordner erstellen
                exportPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), System.AppDomain.CurrentDomain.FriendlyName);
                _ = Directory.CreateDirectory(exportPath);

                //Speicherort für die Vorlagen
                vorlagenPath = System.IO.Path.Combine(exportPath, "Vorlagen");

                //Speicherort für die Wertungsbögen
                wertungsbogenPath = System.IO.Path.Combine(exportPath, "Wertungsbögen");

            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Auswertung konnte nicht geladen werden\n{ex}", "Fehler: Auswertung", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void ExportPDFPGeburtstagsliste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                List<PersonTeilnehmendenliste> personenMitGeburtstag = viewModel.personenMitGeburtstagBeimWettbewerb();
                string htmlGeburtstagsliste_Vorlage = LagerInsights.Properties.Resources.GeburtstagsListe;
                string htmlGeburtstagslisteTabellenzeile_Vorlage = LagerInsights.Properties.Resources.GeburtstagsListeTabellenzeile;
                PDF pDF = new PDF();
                List<string> pfade = new List<string>();

                Settings einstellungen = viewModel.Einstellungen;

                //Logo und Titel direkt in der Vorlage ändern
                if (File.Exists(einstellungen.Logopfad))
                {
                    htmlGeburtstagsliste_Vorlage = htmlGeburtstagsliste_Vorlage.Replace("{logo}", $"data:image/jpeg;base64,{Bilder.readBase64(einstellungen.Logopfad)}");
                }
                else
                {
                    htmlGeburtstagsliste_Vorlage = htmlGeburtstagsliste_Vorlage.Replace("{logo}", $"data:image/svg+xml;base64,{Convert.ToBase64String(LagerInsights.Properties.Resources.Deutsche_Jugendfeuerwehr)}");
                }
                htmlGeburtstagsliste_Vorlage = htmlGeburtstagsliste_Vorlage.Replace("{datum_heute}", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                htmlGeburtstagsliste_Vorlage = htmlGeburtstagsliste_Vorlage.Replace("{titel}", einstellungen.Veranstaltungstitel);
                htmlGeburtstagsliste_Vorlage = htmlGeburtstagsliste_Vorlage.Replace("{datum_veranstaltung}", einstellungen.Veranstaltungsdatum.ToString("dd.MM.yyyy"));
                htmlGeburtstagsliste_Vorlage = htmlGeburtstagsliste_Vorlage.Replace("{ort}", einstellungen.Veranstaltungsort);


                string tabelle = string.Empty;

                int anzahlSeiten = 1;
                int maxProSeite = 26;//26 Passen auf eine Seite
                int seitenindex = 1;
                int alleSeiten = Convert.ToInt32(Math.Ceiling(personenMitGeburtstag.Count() / (float)maxProSeite));

                foreach (PersonTeilnehmendenliste person in personenMitGeburtstag)
                {
                    if (seitenindex >= maxProSeite)
                    {
                        string geburtstagslisteHTML = htmlGeburtstagsliste_Vorlage;
                        geburtstagslisteHTML = geburtstagslisteHTML.Replace("{tabellenzeile}", tabelle);
                        geburtstagslisteHTML = geburtstagslisteHTML.Replace("{akt_seite}", anzahlSeiten.ToString());
                        geburtstagslisteHTML = geburtstagslisteHTML.Replace("{alle_seiten}", alleSeiten.ToString());
                        string pfadinIf = System.IO.Path.Combine(exportPath, $"{Guid.NewGuid()}.pdf");
                        if (!await pDF.ConvertHtmlFileToPdf(geburtstagslisteHTML, pfadinIf, false))
                        {
                            MessageBox.Show($"Export der Geburtstagsliste fehlgeschlagen!", "Fehler: Export Geburtstagsliste", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        pfade.Add(pfadinIf);

                        //Alles wieder zurücksetzen
                        tabelle = string.Empty;
                        seitenindex = 1;
                        anzahlSeiten++;
                    }

                    string currentTabellenzeile = htmlGeburtstagslisteTabellenzeile_Vorlage;
                    currentTabellenzeile = currentTabellenzeile.Replace("{name}", $"{person.Person.Vorname} {person.Person.Nachname}");
                    currentTabellenzeile = currentTabellenzeile.Replace("{feuerwehr}", $"{person.Feuerwehr}");
                    currentTabellenzeile = currentTabellenzeile.Replace("{alter}", $"{person.Person.Alter}");
                    currentTabellenzeile = currentTabellenzeile.Replace("{geburtsdatum}", $"{person.Person.Geburtsdatum.ToShortDateString()}");

                    tabelle += currentTabellenzeile;
                    seitenindex++;
                }


                //Letzte Seite direkt in die Vorlage einfügen und in die Liste packen.
                htmlGeburtstagsliste_Vorlage = htmlGeburtstagsliste_Vorlage.Replace("{tabellenzeile}", tabelle);
                htmlGeburtstagsliste_Vorlage = htmlGeburtstagsliste_Vorlage.Replace("{akt_seite}", anzahlSeiten.ToString());
                htmlGeburtstagsliste_Vorlage = htmlGeburtstagsliste_Vorlage.Replace("{alle_seiten}", anzahlSeiten.ToString());
                string pfad = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
                bool erfolgreich = await pDF.ConvertHtmlFileToPdf(htmlGeburtstagsliste_Vorlage, pfad, false);
                if (!erfolgreich)
                {
                    MessageBox.Show($"Export der Geburtstagsliste fehlgeschlagen!", "Fehler: Export Geburtstagsliste", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                pfade.Add(pfad);
                //Alle Geburtstagslisten in eine Datei speichern und die anderen Dateien löschen
                pDF.MergePdfFiles(pfade, System.IO.Path.Combine(exportPath, $"Geburtstagsliste.pdf"), deleteSource: true, titel: "Geburtstagsliste", subject: "Liste von Teilnehmenden die am Veranstaltungstag Geburtstag haben", author: einstellungen.Veranstaltungsleitung);

                ShowExportMessageBox("Export der Geburtstagsliste abgeschlossen!\nZielverzeichnis öffnen?",
                    "Export Geburtstagsliste", exportPath);
                ((Button)sender).IsEnabled = true;
            }
            catch (Exception ex)
            {
                ((Button)sender).IsEnabled = true;
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der Geburtstagsliste fehlgeschlagen!\n{ex}", "Fehler: Export Geburtstagsliste", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        private async void ExportPDFGruppenliste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                List<Task<bool>> tasks = new List<Task<bool>>
                {
                    helperExportPDFPlatzierungsliste(viewModel.Gruppen.OrderBy(x => x.Feuerwehr).ToList(), "Gruppenliste"),
                    helperExportPDFPlatzierungsliste(viewModel.Gruppen.OrderByDescending(x => x.Feuerwehr).ToList(), "GruppenlisteAbsteigend")
                };
                await Task.WhenAll(tasks);

                if (tasks.All(task => task.Result))
                {
                    ShowExportMessageBox("Export der Platzierungslisten abgeschlossen!\nZielverzeichnis öffnen?",
                        "Export Platzierungslisten", exportPath);
                }
                ((Button)sender).IsEnabled = true;

            }
            catch (Exception ex)
            {
                ((Button)sender).IsEnabled = true;
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der Platzierungslisten fehlgeschlagen!\n{ex}", "Fehler: Export Platzierungslisten", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        private async Task<bool> helperExportPDFPlatzierungsliste(List<Jugendfeuerwehr> gruppen, string dateiname)
        {

            try
            {
                string htmlPlatzierungsliste_Vorlage = LagerInsights.Properties.Resources.PlatzierungsListe;
                string htmlPlatzierungslisteTabellenzeile_Vorlage = LagerInsights.Properties.Resources.PlatzierungsListeTabellenzeile;
                PDF pDF = new PDF();
                List<string> pfade = new List<string>();
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                Settings einstellungen = viewModel.Einstellungen;

                //Logo und Titel direkt in der Vorlage ändern
                if (File.Exists(einstellungen.Logopfad))
                {
                    htmlPlatzierungsliste_Vorlage = htmlPlatzierungsliste_Vorlage.Replace("{logo}", $"data:image/jpeg;base64,{Bilder.readBase64(einstellungen.Logopfad)}");
                }
                else
                {
                    htmlPlatzierungsliste_Vorlage = htmlPlatzierungsliste_Vorlage.Replace("{logo}", $"data:image/svg+xml;base64,{Convert.ToBase64String(LagerInsights.Properties.Resources.Deutsche_Jugendfeuerwehr)}");
                }
                htmlPlatzierungsliste_Vorlage = htmlPlatzierungsliste_Vorlage.Replace("{datum_heute}", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                htmlPlatzierungsliste_Vorlage = htmlPlatzierungsliste_Vorlage.Replace("{titel}", einstellungen.Veranstaltungstitel);
                htmlPlatzierungsliste_Vorlage = htmlPlatzierungsliste_Vorlage.Replace("{datum_veranstaltung}", einstellungen.Veranstaltungsdatum.ToString("dd.MM.yyyy"));
                htmlPlatzierungsliste_Vorlage = htmlPlatzierungsliste_Vorlage.Replace("{ort}", einstellungen.Veranstaltungsort);


                string tabelle = string.Empty;

                int anzahlSeiten = 1;
                int maxProSeite = 25;//25 Passen auf eine Seite
                int seitenindex = 1;
                int alleSeiten = Convert.ToInt32(Math.Ceiling(viewModel.Gruppen.Count() / (float)maxProSeite));

                foreach (Jugendfeuerwehr gruppe in gruppen)
                {
                    if (seitenindex > maxProSeite)
                    {
                        string platzierungslisteHTML = htmlPlatzierungsliste_Vorlage;
                        platzierungslisteHTML = platzierungslisteHTML.Replace("{tabellenzeile}", tabelle);
                        platzierungslisteHTML = platzierungslisteHTML.Replace("{akt_seite}", anzahlSeiten.ToString());
                        platzierungslisteHTML = platzierungslisteHTML.Replace("{alle_seiten}", alleSeiten.ToString());
                        string pfadinIf = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
                        if (!await pDF.ConvertHtmlFileToPdf(platzierungslisteHTML, pfadinIf, false))
                        {
                            MessageBox.Show($"Export der Platzierungsliste fehlgeschlagen!", "Fehler: Export Platzierungsliste", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                        pfade.Add(pfadinIf);

                        //Alles wieder zurücksetzen
                        tabelle = string.Empty;
                        seitenindex = 1;
                        anzahlSeiten++;
                    }

                    string currentTabellenzeile = htmlPlatzierungslisteTabellenzeile_Vorlage;
                    currentTabellenzeile = currentTabellenzeile.Replace("{lagernr}", $"{gruppe.LagerNr}.");
                    currentTabellenzeile = currentTabellenzeile.Replace("{gruppenname}", $"{gruppe.Feuerwehr}");
                    currentTabellenzeile = currentTabellenzeile.Replace("{ort}", $"{gruppe.Organisationseinheit}");
                    currentTabellenzeile = currentTabellenzeile.Replace("{teilnehmende}", $"{gruppe.AnzahlTeilnehmer}");

                    tabelle += currentTabellenzeile;
                    seitenindex++;
                }


                //Letzte Seite direkt in die Vorlage einfügen und in die Liste packen.
                htmlPlatzierungsliste_Vorlage = htmlPlatzierungsliste_Vorlage.Replace("{tabellenzeile}", tabelle);
                htmlPlatzierungsliste_Vorlage = htmlPlatzierungsliste_Vorlage.Replace("{akt_seite}", anzahlSeiten.ToString());
                htmlPlatzierungsliste_Vorlage = htmlPlatzierungsliste_Vorlage.Replace("{alle_seiten}", anzahlSeiten.ToString());
                string pfad = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
                bool erfolgreich = await pDF.ConvertHtmlFileToPdf(htmlPlatzierungsliste_Vorlage, pfad, false);
                if (!erfolgreich)
                {
                    MessageBox.Show($"Export der Platzierungslisten fehlgeschlagen!", "Fehler: Export Platzierungslisten", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                pfade.Add(pfad);
                //Alle Platzierungslisten in eine Datei speichern und die anderen Dateien löschen
                pDF.MergePdfFiles(pfade, System.IO.Path.Combine(exportPath, $"{dateiname}.pdf"), true, titel: "Platzierungsliste aller Gruppen",
                    subject: $"Platzierungsliste für den {einstellungen.Veranstaltungstitel} am {einstellungen.Veranstaltungsdatum.ToShortDateString()} in {einstellungen.Veranstaltungsort}.",
                    author: einstellungen.Veranstaltungsleitung);
                return true;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der Platzierungslisten fehlgeschlagen!\n{ex}", "Fehler: Export Platzierungslisten", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async void ExportUrkunden_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                Settings einstellungen = viewModel.Einstellungen;

                PDF pDF = new PDF();
                //Für die Excel Liste die Leere Datei erstellen
                string excelpath = System.IO.Path.Combine(exportPath, "Urkundenliste.xlsx");
                WriteFile.ByteArrayToFile(excelpath, LagerInsights.Properties.Resources.Urkundenliste);

                string urkundeOverlayPfad = System.IO.Path.Combine(vorlagenPath, "UrkundeOverlay.html");
                string urkundeOverlay = string.Empty;
                if (File.Exists(urkundeOverlayPfad))
                {
                    urkundeOverlay = File.ReadAllText(urkundeOverlayPfad);
                }
                else
                {
                    urkundeOverlay = LagerInsights.Properties.Resources.UrkundeOverlay; //default
                    MessageBox.Show("Die Vorlage für die Urkunde wurde nicht gefunden. Es wird der Standard benutzt.", "Export Urkunde", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                //Alles für die Excel Urkundenliste
                //bool erfolgreichExcel = Excel.WriteUrkundeToExcel(excelpath, viewModel.Gruppen.OrderByDescending(x => x.Feuerwehr).ToList());
                //if (!erfolgreichExcel)
               // {
                //    MessageBox.Show($"Export der Urkunden fehlgeschlagen!", "Fehler: Export Urkunde", MessageBoxButton.OK, MessageBoxImage.Error);
                //    return;
                //}

                //Allgemeines ersetzen
                urkundeOverlay = urkundeOverlay.Replace("{veranstaltungstitel}", viewModel.Einstellungen.Veranstaltungstitel);
                urkundeOverlay = urkundeOverlay.Replace("{veranstaltungsort}", viewModel.Einstellungen.Veranstaltungsort);
                urkundeOverlay = urkundeOverlay.Replace("{veranstaltungsleitung}", viewModel.Einstellungen.Veranstaltungsleitung);
                urkundeOverlay = urkundeOverlay.Replace("{veranstaltungsdatum}", viewModel.Einstellungen.Veranstaltungsdatum.ToString("d"));
                urkundeOverlay = urkundeOverlay.Replace("{namelinks}", viewModel.Einstellungen.Namelinks);
                urkundeOverlay = urkundeOverlay.Replace("{namerechts}", viewModel.Einstellungen.Namerechts);
                urkundeOverlay = urkundeOverlay.Replace("{funktionlinks}", viewModel.Einstellungen.Funktionlinks);
                urkundeOverlay = urkundeOverlay.Replace("{funktionrechts}", viewModel.Einstellungen.Funktionrechts);

                if (File.Exists(viewModel.Einstellungen.Unterschriftlinks))
                {
                    urkundeOverlay = urkundeOverlay.Replace("{unterschriftlinks}", $"data:image/jpeg;base64,{Bilder.readBase64(viewModel.Einstellungen.Unterschriftlinks)}");
                    urkundeOverlay = urkundeOverlay.Replace("{unterschriftrechts}", $"data:image/jpeg;base64,{Bilder.readBase64(viewModel.Einstellungen.Unterschriftrechts)}");
                }

                List<string> pfade = new List<string>();
                foreach (Jugendfeuerwehr gruppe in viewModel.Gruppen.OrderByDescending(x => x.Feuerwehr))
                {
                    string aktuelleUrkunde = urkundeOverlay;

                    aktuelleUrkunde = aktuelleUrkunde.Replace("{jugendfeuerwehr}", gruppe.Feuerwehr);
                    aktuelleUrkunde = aktuelleUrkunde.Replace("{platz}", gruppe.LagerNr.ToString()); //TODO Urkunde Umbauen auf Allgemeine Urkunde für den Teilnehmer

                    string pfad = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
                    bool erfolgreich = await pDF.ConvertHtmlFileToPdf(aktuelleUrkunde, pfad, false);
                    if (!erfolgreich)
                    {
                        MessageBox.Show($"Export der Urkunden fehlgeschlagen!", "Fehler: Export Urkunde", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    pfade.Add(pfad);

                }
                pDF.MergePdfFiles(pfade, System.IO.Path.Combine(exportPath, $"UrkundenOverlay.pdf"), true, "Urkunden", subject: "Urkunden", author: einstellungen.Veranstaltungsleitung);


                ShowExportMessageBox("Export der Urkunden abgeschlossen!\nZielverzeichnis öffnen?",
                    "Export Urkunden", exportPath);
                ((Button)sender).IsEnabled = true;
            }
            catch (Exception ex)
            {
                ((Button)sender).IsEnabled = true;
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der Urkunden fehlgeschlagen!\n{ex}", "Fehler: Export Urkunden", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       

        private void ExportGruppenExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                Excel.ExportExcelGruppen(viewModel.Gruppen.OrderBy(x => x.Feuerwehr).ToList(), exportPath);

                ShowExportMessageBox("Export der Gruppen abgeschlossen!\nZielverzeichnis öffnen?",
                    "Export Gruppen", exportPath);
                ((Button)sender).IsEnabled = true;
            }
            catch (Exception ex)
            {
                ((Button)sender).IsEnabled = true;
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der Gruppen fehlgeschlagen!\n{ex}", "Fehler: Export Gruppendaten", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ExportUrkundenvorlage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;

                _ = Directory.CreateDirectory(vorlagenPath);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(vorlagenPath, "Urkunde_Druckvorlage.pdf"), LagerInsights.Properties.Resources.UrkundeDruckTheme1);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(vorlagenPath, "Urkunde_Original.indd"), LagerInsights.Properties.Resources.UrkundeOriginalTheme1);
                WriteFile.writeText(System.IO.Path.Combine(vorlagenPath, "UrkundeOverlay.html"), LagerInsights.Properties.Resources.UrkundeOverlay);
                WriteFile.writeText(System.IO.Path.Combine(vorlagenPath, "UrkundeOverlayTheme1.html"), LagerInsights.Properties.Resources.UrkundeOverlayTheme1);
                WriteFile.writeText(System.IO.Path.Combine(vorlagenPath, "UrkundeOverlayJuengsteGruppe.html"), LagerInsights.Properties.Resources.UrkundeOverlayJuengsteGruppe);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(vorlagenPath, "Urkundenpapier-Beispiel.pdf"), LagerInsights.Properties.Resources.Urkundenpapier_BeispielDruck);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(vorlagenPath, "Urkundenpapier-Beispiel.indd"), LagerInsights.Properties.Resources.Urkundenpapier_BeispielIndesign);

                ShowExportMessageBox("Export der Urkundenvorlage abgeschlossen!\nZielverzeichnis öffnen?",
                    "Export Urkundenvorlage", vorlagenPath);
                ((Button)sender).IsEnabled = true;

            }
            catch (Exception ex)
            {
                ((Button)sender).IsEnabled = true;

                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der Urkundenvorlage fehlgeschlagen!\n{ex}", "Fehler: Export Urkundenvorlage", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowExportMessageBox(string message, string title, string path)
        {
            try
            {
                if (MessageBox.Show(message, title,
                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)

                {
                    Process.Start(Environment.GetEnvironmentVariable("WINDIR") +
                                  @"\explorer.exe", path);
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
                double minWindowSize = 1020; // Minimale Fensterbreite

                // Berechne den Skalierungsfaktor basierend auf der aktuellen Fensterbreite
                double scaleFactor = Math.Min(1, ActualWidth / minWindowSize);

                // Setze den Skalierungsfaktor im ViewModel
                viewModel.ScaleFactorEvaluation = scaleFactor;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }
    }

    public class WidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                return width - 10;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
