using BWB_Auswertung.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using BWB_Auswertung.IO;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BWB_Auswertung.Views
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
                    ((x.ATeilGesamteindruck > 0)
                    && (x.BTeilGesamteindruck > 0)
                    && (x.PunkteBTeil > 0)
                    && (x.DurchschnittszeitBTeil > 0)
                    && (x.DurchschnittszeitATeil > 0)
                    && (x.DurchschnittszeitKnotenATeil > 0)
                    && (x.SollZeitBTeilInSekunden > 0))
                    ||
                    x.OhneWertung==true
                    ).ToList().Count();
                    double prozentsatz = prozentwert / grundwert * 100d;
                    Wettbewerbsfortschritt.Value = prozentsatz;
                    AnzahlFehlenderGruppen.Content = grundwert - prozentwert;
                    GesamtanzahlGruppen.Content = grundwert;
                }
                catch (Exception ex)
                {
                    //Noch keine Daten zum auswerten vorhanden
                    Wettbewerbsfortschritt.Value = 0;
                    AnzahlFehlenderGruppen.Content = grundwert;
                    GesamtanzahlGruppen.Content = grundwert;
                    LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Warning);
                }
                try
                {

                    //Jüngste Gruppe
                    var juengsteGruppe = mainViewModel.Gruppen.OrderBy(x => x.GesamtAlterinTagen).First();
                    JuengsteGruppe.Content = juengsteGruppe.GruppenName;
                    JuengsteGruppeAlter.Content = juengsteGruppe.GesamtAlter;

                    //Älteste Gruppe
                    var aeltesteGruppe = mainViewModel.Gruppen.OrderByDescending(x => x.GesamtAlterinTagen).First();
                    AeltesteGruppe.Content = aeltesteGruppe.GruppenName;
                    AeltesteGruppeAlter.Content = aeltesteGruppe.GesamtAlter;


                    //Bester A-Teil
                    var besterATeilGruppe = mainViewModel.Gruppen.OrderByDescending(x => x.PunkteATeil).First();
                    BesterATeilGruppe.Content = besterATeilGruppe.GruppenName;
                    BesterATeilGruppePunkte.Content = besterATeilGruppe.PunkteATeil;

                    //Schnellster A-Teil
                    var schnellsterATeilGruppe = mainViewModel.Gruppen.Where(x => x.DurchschnittszeitATeil > 0).OrderBy(x => x.DurchschnittszeitATeil).First();
                    SchnellsterATeilGruppe.Content = schnellsterATeilGruppe.GruppenName;
                    TimeSpan ateil = new TimeSpan(0, 0, Convert.ToInt32(schnellsterATeilGruppe.DurchschnittszeitATeil));
                    SchnellsterATeilGruppeZeit.Content = $"{ateil.Minutes}:{ateil.Seconds}";

                    //Schnellste Knotenzeit
                    var schnellsteKnotenZeitGruppe = mainViewModel.Gruppen.Where(x => x.DurchschnittszeitKnotenATeil > 0).OrderBy(x => x.DurchschnittszeitKnotenATeil).First();
                    SchnellsteKnotenZeitGruppe.Content = schnellsteKnotenZeitGruppe.GruppenName;
                    SchnellsteKnotenZeitGruppeZeit.Content = schnellsteKnotenZeitGruppe.DurchschnittszeitKnotenATeil;

                    //Bester B-Teil
                    var besterBTeilGruppe = mainViewModel.Gruppen.OrderByDescending(x => x.PunkteBTeil).First();
                    BesterBTeilGruppe.Content = besterBTeilGruppe.GruppenName;
                    BesterBTeilGruppePunkte.Content = besterBTeilGruppe.PunkteBTeil;

                    //Schnellster B-Teil
                    var schnellsterBTeilGruppe = mainViewModel.Gruppen.Where(x => x.DurchschnittszeitBTeil > 0).OrderBy(x => x.DurchschnittszeitBTeil).First();
                    SchnellsterBTeilGruppe.Content = schnellsterBTeilGruppe.GruppenName;
                    TimeSpan bteil = new TimeSpan(0, 0, Convert.ToInt32(schnellsterBTeilGruppe.DurchschnittszeitBTeil));
                    SchnellsterBTeilGruppeZeit.Content = $"{bteil.Minutes}:{bteil.Seconds}";
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

        private async void ExportKontrollblaetter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                string htmlKontrollblaetter_Vorlage = BWB_Auswertung.Properties.Resources.Kontrollblatt;
                string htmlKontrollblaetterTabellenzeile_Vorlage = BWB_Auswertung.Properties.Resources.KontrollblattTabellenzeile;
                PDF pDF = new PDF();
                //List<string> pdfKontrollblaetter = new List<string>();
                List<string> pfade = new List<string>();

                MainViewModel viewModel = (MainViewModel)this.DataContext;
                Settings einstellungen = viewModel.Einstellungen;

                foreach (Gruppe gruppe in viewModel.Gruppen)
                {
                    string kontrollblattGruppe = htmlKontrollblaetter_Vorlage;

                    //Allgemeine Daten auf Kontrollblatt setzen
                    if (File.Exists(einstellungen.Logopfad))
                    {
                        kontrollblattGruppe = kontrollblattGruppe.Replace("{logo}", $"data:image/jpeg;base64,{Bilder.readBase64(einstellungen.Logopfad)}");
                    }
                    else
                    {
                        kontrollblattGruppe = kontrollblattGruppe.Replace("{logo}", $"data:image/svg+xml;base64,{Convert.ToBase64String(BWB_Auswertung.Properties.Resources.Deutsche_Jugendfeuerwehr)}");
                    }
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{titelderVeranstaltung}", einstellungen.Veranstaltungstitel);
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{jugendfeuerwehr}", gruppe.Feuerwehr);
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{gruppenname}", gruppe.GruppenName);
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{gesamtalter}", gruppe.GesamtAlter.ToString());
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{sollzeitb}", gruppe.SollZeitBTeilInMinutenString);

                    kontrollblattGruppe = kontrollblattGruppe.Replace("{veranstaltungsdatum}", einstellungen.Veranstaltungsdatum.ToShortDateString());
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{ort}", einstellungen.Veranstaltungsort);
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{organisationseinheit}", gruppe.Organisationseinheit);
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{startnummer}", gruppe.StartNr.ToString());
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{startzeita}", gruppe.StartzeitATeil.ToShortTimeString());
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{startzeitb}", gruppe.StartzeitBTeil.ToShortTimeString());
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{bahnnummera}", gruppe.WettbewerbsbahnATeil.ToString());
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{bahnnummerb}", gruppe.WettbewerbsbahnBTeil.ToString());
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{datum_heute}", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    string tabellenzeilen = string.Empty;
                    //Tabellenzeile für die Personen erstellen
                    for (int i = 0; i <= 9; i++)
                    {
                        string tabellenzeileCurrent = htmlKontrollblaetterTabellenzeile_Vorlage;
                        if (i == 9)
                        {
                            tabellenzeileCurrent = tabellenzeileCurrent.Replace("{nr}", "E");
                        }
                        else
                        {
                            tabellenzeileCurrent = tabellenzeileCurrent.Replace("{nr}", (i + 1).ToString());
                        }
                        tabellenzeileCurrent = tabellenzeileCurrent.Replace("{vorname}", gruppe.Persons[i].Vorname);
                        tabellenzeileCurrent = tabellenzeileCurrent.Replace("{nachname}", gruppe.Persons[i].Nachname);
                        tabellenzeileCurrent = tabellenzeileCurrent.Replace("{geschlecht}", gruppe.Persons[i].Geschlecht.ToString());
                        tabellenzeileCurrent = tabellenzeileCurrent.Replace("{geburtsdatum}", gruppe.Persons[i].Geburtsdatum.ToShortDateString());
                        tabellenzeileCurrent = tabellenzeileCurrent.Replace("{alter}", gruppe.Persons[i].Alter.ToString());
                        tabellenzeilen += tabellenzeileCurrent;
                    }
                    //Fertige Tabellenzeilen einfügen
                    kontrollblattGruppe = kontrollblattGruppe.Replace("{tabellenzeile}", tabellenzeilen);

                    string pfad = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
                    bool erfolgreich = await pDF.ConvertHtmlFileToPdf(kontrollblattGruppe, pfad, false);
                    if (!erfolgreich)
                    {
                        MessageBox.Show($"Export der Kontrollblätter fehlgeschlagen!", "Fehler: Export Kontrollblätter", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    pfade.Add(pfad);
                }

                //Alle Kontrollblätter in eine Datei speichern und die anderen Dateien löschen
                pDF.MergePdfFiles(pfade, System.IO.Path.Combine(exportPath, $"Kontrollblätter.pdf"), true,
                    titel: $"Kontrollblätter - Stand: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}",
                    subject: $"Kontrollblätter für den {einstellungen.Veranstaltungstitel}",
                    author: einstellungen.Veranstaltungsleitung);


                //Excel Datei für die Checkup Zelte exportieren
                string excelpath = System.IO.Path.Combine(exportPath, "Kontrolle-Check-Up-Zelt.xlsx");
                WriteFile.ByteArrayToFile(excelpath, BWB_Auswertung.Properties.Resources.CheckUpZelt);

                bool erfolgreichExcel = Excel.WriteCheckUpToExcel(excelpath, einstellungen);
                if (!erfolgreichExcel)
                {
                    MessageBox.Show($"Export der Kontrollblätter fehlgeschlagen!", "Fehler: Export Kontrollblätter", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ShowExportMessageBox("Export der Kontrollblätter abgeschlossen!\nZielverzeichnis öffnen?",
                    "Export Kontrollblätter", exportPath);
                ((Button)sender).IsEnabled = true;
            }
            catch (Exception ex)
            {
                ((Button)sender).IsEnabled = true;
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der Kontrollblätter fehlgeschlagen!\n{ex}", "Fehler: Export Kontrollblätter", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportPDFPGeburtstagsliste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                List<PersonTeilnehmendenliste> personenMitGeburtstag = viewModel.personenMitGeburtstagBeimWettbewerb();
                string htmlGeburtstagsliste_Vorlage = BWB_Auswertung.Properties.Resources.GeburtstagsListe;
                string htmlGeburtstagslisteTabellenzeile_Vorlage = BWB_Auswertung.Properties.Resources.GeburtstagsListeTabellenzeile;
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
                    htmlGeburtstagsliste_Vorlage = htmlGeburtstagsliste_Vorlage.Replace("{logo}", $"data:image/svg+xml;base64,{Convert.ToBase64String(BWB_Auswertung.Properties.Resources.Deutsche_Jugendfeuerwehr)}");
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
        private async void ExportPDFPlatzierungsliste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                List<Task<bool>> tasks = new List<Task<bool>>
                {
                    helperExportPDFPlatzierungsliste(viewModel.Gruppen.OrderBy(x => x.Platz).ToList(), "Platzierungsliste"),
                    helperExportPDFPlatzierungsliste(viewModel.Gruppen.OrderByDescending(x => x.Platz).ToList(), "PlatzierungslisteAbsteigend")
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
        private async Task<bool> helperExportPDFPlatzierungsliste(List<Gruppe> gruppen, string dateiname)
        {

            try
            {
                string htmlPlatzierungsliste_Vorlage = BWB_Auswertung.Properties.Resources.PlatzierungsListe;
                string htmlPlatzierungslisteTabellenzeile_Vorlage = BWB_Auswertung.Properties.Resources.PlatzierungsListeTabellenzeile;
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
                    htmlPlatzierungsliste_Vorlage = htmlPlatzierungsliste_Vorlage.Replace("{logo}", $"data:image/svg+xml;base64,{Convert.ToBase64String(BWB_Auswertung.Properties.Resources.Deutsche_Jugendfeuerwehr)}");
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

                foreach (Gruppe gruppe in gruppen)
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
                    currentTabellenzeile = currentTabellenzeile.Replace("{platz}", $"{gruppe.Platz}.");
                    currentTabellenzeile = currentTabellenzeile.Replace("{gruppenname}", $"{gruppe.GruppenName}");
                    currentTabellenzeile = currentTabellenzeile.Replace("{ort}", $"{gruppe.Organisationseinheit}");
                    currentTabellenzeile = currentTabellenzeile.Replace("{gesamtpunkte}", $"{gruppe.GesamtPunkte}");

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

        private void ExportExcelPlatzierungsliste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                string excelpath = System.IO.Path.Combine(exportPath, "Platzierungsliste.xlsx");
                WriteFile.ByteArrayToFile(excelpath, BWB_Auswertung.Properties.Resources.PlatzierungslisteExcel);

                //Alles für die Excel Siegerliste
                bool erfolgreichExcel = Excel.WritePlatzierungslisteToExcel(excelpath, viewModel.Gruppen.OrderBy(x => x.Platz).ToList());
                if (!erfolgreichExcel)
                {
                    MessageBox.Show($"Export der Platzierungsliste fehlgeschlagen!", "Fehler: Export Platzierungsliste", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ShowExportMessageBox("Export der Platzierungsliste abgeschlossen!\nZielverzeichnis öffnen?",
                    "Export Platzierungsliste", exportPath);
                ((Button)sender).IsEnabled = true;
            }
            catch (Exception ex)
            {
                ((Button)sender).IsEnabled = true;
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der Excel Platzierungsliste fehlgeschlagen!\n{ex}", "Fehler: Export Platzierungslisten", MessageBoxButton.OK, MessageBoxImage.Error);
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
                WriteFile.ByteArrayToFile(excelpath, BWB_Auswertung.Properties.Resources.Urkundenliste);

                string urkundeOverlayPfad = System.IO.Path.Combine(vorlagenPath, "UrkundeOverlay.html");
                string urkundeOverlay = string.Empty;
                if (File.Exists(urkundeOverlayPfad))
                {
                    urkundeOverlay = File.ReadAllText(urkundeOverlayPfad);
                }
                else
                {
                    urkundeOverlay = BWB_Auswertung.Properties.Resources.UrkundeOverlay; //default
                    MessageBox.Show("Die Vorlage für die Urkunde wurde nicht gefunden. Es wird der Standard benutzt.", "Export Urkunde", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                //Alles für die Excel Urkundenliste
                bool erfolgreichExcel = Excel.WriteUrkundeToExcel(excelpath, viewModel.Gruppen.OrderByDescending(x => x.Platz).ToList());
                if (!erfolgreichExcel)
                {
                    MessageBox.Show($"Export der Urkunden fehlgeschlagen!", "Fehler: Export Urkunde", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

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
                foreach (Gruppe gruppe in viewModel.Gruppen.OrderByDescending(x => x.Platz))
                {
                    string aktuelleUrkunde = urkundeOverlay;

                    aktuelleUrkunde = aktuelleUrkunde.Replace("{jugendfeuerwehr}", gruppe.GruppenName);
                    aktuelleUrkunde = aktuelleUrkunde.Replace("{platz}", gruppe.Platz.ToString());

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

        private async void ExportUrkundeJuengsteGruppe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                Settings einstellungen = viewModel.Einstellungen;

                PDF pDF = new PDF();

                string urkundeOverlayPfad = System.IO.Path.Combine(vorlagenPath, "UrkundeOverlayJuengsteGruppe.html");
                string urkundeOverlay = string.Empty;
                if (File.Exists(urkundeOverlayPfad))
                {
                    urkundeOverlay = File.ReadAllText(urkundeOverlayPfad);
                }
                else
                {
                    urkundeOverlay = BWB_Auswertung.Properties.Resources.UrkundeOverlayJuengsteGruppe; //default
                    MessageBox.Show("Die Vorlage für die Urkunde(Jüngste Gruppe) wurde nicht gefunden. Es wird der Standard benutzt.", "Export Urkunde", MessageBoxButton.OK, MessageBoxImage.Information);
                }

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

                var juengsteGruppe = viewModel.Gruppen.OrderBy(x => x.GesamtAlterinTagen).First();

                urkundeOverlay = urkundeOverlay.Replace("{jugendfeuerwehr}", juengsteGruppe.GruppenName);
                urkundeOverlay = urkundeOverlay.Replace("{jahre}", juengsteGruppe.GesamtAlter.ToString());

                string pfad = System.IO.Path.Combine(exportPath, $"UrkundeJuengsteGruppe.pdf");
                bool erfolgreich = await pDF.ConvertHtmlFileToPdf(urkundeOverlay, pfad, true, $"Jüngste Gruppe: {juengsteGruppe.GruppenName}", $"Urkunde für die jüngste Gruppe", einstellungen.Veranstaltungsleitung);

                if (!erfolgreich)
                {
                    MessageBox.Show($"Export der jüngsten Gruppe fehlgeschlagen!", "Fehler: Export jüngste Gruppe", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ShowExportMessageBox("Export der jüngsten Gruppe abgeschlossen!\nZielverzeichnis öffnen?",
                    "Export Urkunden", exportPath);
                ((Button)sender).IsEnabled = true;
            }
            catch (Exception ex)
            {
                ((Button)sender).IsEnabled = true;
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der jüngsten Gruppe fehlgeschlagen!\n{ex}", "Fehler: Export jüngste Gruppe", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportGruppenExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                MainViewModel viewModel = (MainViewModel)this.DataContext;
                Excel.ExportExcelGruppen(viewModel.Gruppen.OrderBy(x => x.Platz).ToList(), exportPath);

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

        private void ExportWettbewerbsordnung_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                string speicherordner = System.IO.Path.Combine(exportPath, "Wettbewerbsordnung");
                _ = Directory.CreateDirectory(speicherordner);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(speicherordner, "DJF_Wettbewerbsordnung_BWB_2013.pdf"), BWB_Auswertung.Properties.Resources.DJF_Wettbewerbsordnung_BWB_2013);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(speicherordner, "Aktuelles_BWB_2016.pdf"), BWB_Auswertung.Properties.Resources.Aktuelles_BWB_2016);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(speicherordner, "Wettbewerbsinfo.pdf"), BWB_Auswertung.Properties.Resources.Wettbewerbsinfo);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(speicherordner, "Wettbewerbsrichtlinien.pdf"), BWB_Auswertung.Properties.Resources.Wettbewerbsrichtlinien);

                ShowExportMessageBox("Export der Wettbewerbsordnung abgeschlossen!\nZielverzeichnis öffnen?",
                    "Export Wettbewerbsordnung", speicherordner);
                ((Button)sender).IsEnabled = true;
            }
            catch (Exception ex)
            {
                ((Button)sender).IsEnabled = true;
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der Wettbewerbsordnung fehlgeschlagen!\n{ex}", "Fehler: Export Wettbewerbsordnung", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportUrkundenvorlage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;

                _ = Directory.CreateDirectory(vorlagenPath);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(vorlagenPath, "Urkunde_Druckvorlage.pdf"), BWB_Auswertung.Properties.Resources.UrkundeDruckTheme1);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(vorlagenPath, "Urkunde_Original.indd"), BWB_Auswertung.Properties.Resources.UrkundeOriginalTheme1);
                WriteFile.writeText(System.IO.Path.Combine(vorlagenPath, "UrkundeOverlay.html"), BWB_Auswertung.Properties.Resources.UrkundeOverlay);
                WriteFile.writeText(System.IO.Path.Combine(vorlagenPath, "UrkundeOverlayTheme1.html"), BWB_Auswertung.Properties.Resources.UrkundeOverlayTheme1);
                WriteFile.writeText(System.IO.Path.Combine(vorlagenPath, "UrkundeOverlayJuengsteGruppe.html"), BWB_Auswertung.Properties.Resources.UrkundeOverlayJuengsteGruppe);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(vorlagenPath, "Urkundenpapier-Beispiel.pdf"), BWB_Auswertung.Properties.Resources.Urkundenpapier_BeispielDruck);
                WriteFile.ByteArrayToFile(System.IO.Path.Combine(vorlagenPath, "Urkundenpapier-Beispiel.indd"), BWB_Auswertung.Properties.Resources.Urkundenpapier_BeispielIndesign);

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

        private void ExportMeldebogenBlanko_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;

                WriteFile.ByteArrayToFile(System.IO.Path.Combine(vorlagenPath, "Meldebogen-Blanko.xlsx"), BWB_Auswertung.Properties.Resources.Meldebogen_Blanko);

                ShowExportMessageBox("Export der Meldebogen Vorlage abgeschlossen!\nZielverzeichnis öffnen?",
                    "Export Meldebogen Blanko", vorlagenPath);
                ((Button)sender).IsEnabled = true;
            }
            catch (Exception ex)
            {
                ((Button)sender).IsEnabled = true;
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der Meldebogenvorlage fehlgeschlagen!\n{ex}", "Fehler: Export Meldebogenvorlage", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportExcelWertungsbogen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                MainViewModel viewModel = (MainViewModel)this.DataContext;

                _ = Directory.CreateDirectory(wertungsbogenPath);
                bool erfolgreichExcel = Excel.WriteWertungsbogenToExcel(wertungsbogenPath, viewModel.Gruppen.OrderByDescending(x => x.Platz).ToList(), viewModel.Einstellungen);
                if (!erfolgreichExcel)
                {
                    MessageBox.Show($"Export der Wertungsbögen fehlgeschlagen!", "Fehler: Export Wertungsbögen", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ShowExportMessageBox("Export der Wertungsbögen abgeschlossen!\nZielverzeichnis öffnen?",
                    "Export Wertungsbögen", exportPath);
                ((Button)sender).IsEnabled = true;

            }
            catch (Exception ex)
            {
                ((Button)sender).IsEnabled = true;
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                MessageBox.Show($"Export der Wertungsbögen fehlgeschlagen!\n{ex}", "Fehler: Export Wertungsbögen", MessageBoxButton.OK, MessageBoxImage.Error);
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
