using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BWB_Auswertung.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace BWB_Auswertung.IO
{
    public static class Excel
    {
        public static Gruppe ImportExcelGruppe(string path)
        {
            try
            {
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook = new XSSFWorkbook(file);
                    ISheet sheet = workbook.GetSheetAt(0);

                    List<Person> teilnehmende = new List<Person>();

                    string jugendfeuerwehr = GetCellValue(sheet, "C", 4).Trim();
                    string ou = GetCellValue(sheet, "C", 5).Trim();

                    for (int i = 13; i <= 22; i++)
                    {
                        string nachname = GetCellValue(sheet, "B", i).Trim();
                        string vorname = GetCellValue(sheet, "C", i).Trim();
                        string tag = GetCellValue(sheet, "D", i);
                        string monat = GetCellValue(sheet, "E", i);
                        string jahr = GetCellValue(sheet, "F", i);

                        DateTime geburtsdatum = DateTime.Now;
                        try
                        {
                            geburtsdatum = new DateTime(
                                Convert.ToInt32(jahr),
                                Convert.ToInt32(monat),
                                Convert.ToInt32(tag));
                        }
                        catch (Exception ex)
                        {
                            LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                        }
                        Person person = new Person()
                        {
                            Vorname = vorname,
                            Nachname = nachname,
                            Geschlecht = Gender.N,
                            Geburtsdatum = geburtsdatum
                        };

                        teilnehmende.Add(person);
                    }

                    Gruppe gruppe = new Gruppe() { Feuerwehr = jugendfeuerwehr, Organisationseinheit = ou, Persons = teilnehmende, GruppenName = jugendfeuerwehr };

                    return gruppe;
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                return null;
            }
        }

        public static void ExportExcelGruppen(List<Gruppe> gruppen, string path)
        {
            try
            {
                string file = Path.Combine(path, $"Gruppendaten.xlsx");
                WriteFile.ByteArrayToFile(file, BWB_Auswertung.Properties.Resources.Gruppendaten);

                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
                {
                    IWorkbook workbook = new XSSFWorkbook(fs);
                    ISheet sheet = workbook.GetSheetAt(0);

                    int index = 3;
                    foreach (Gruppe gruppe in gruppen)
                    {
                        foreach (Person teilnehmende in gruppe.Persons)
                        {
                            SetCellValue(sheet, "A", index, gruppe.Organisationseinheit ?? "");
                            SetCellValue(sheet, "B", index, gruppe.Feuerwehr);
                            SetCellValue(sheet, "C", index, gruppe.GruppenName);
                            SetCellValue(sheet, "D", index, gruppe.StartNr.ToString());
                            SetCellValue(sheet, "E", index, gruppe.Platz.ToString());
                            SetCellValue(sheet, "F", index, gruppe.GesamtPunkte.ToString());
                            SetCellValue(sheet, "G", index, teilnehmende.Geschlecht.ToString());
                            SetCellValue(sheet, "H", index, teilnehmende.Vorname);
                            SetCellValue(sheet, "I", index, teilnehmende.Nachname);
                            SetCellValue(sheet, "J", index, teilnehmende.Geburtsdatum.ToString("yyyy-MM-dd"));
                            SetCellValue(sheet, "K", index, teilnehmende.Alter.ToString());

                            index++;
                        }
                    }

                    using (FileStream writeFileStream = new FileStream(file, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(writeFileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private static void SetCellValue(ISheet sheet, string columnName, int rowIndex, string value)
        {
            IRow row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
            ICell cell = row.GetCell(ColumnNameToIndex(columnName)) ?? row.CreateCell(ColumnNameToIndex(columnName));
            cell.SetCellValue(value);
        }

        private static string GetCellValue(ISheet sheet, string columnName, int rowIndex)
        {
            IRow row = sheet.GetRow(rowIndex);
            ICell cell = row?.GetCell(ColumnNameToIndex(columnName));
            return cell?.ToString() ?? string.Empty;
        }

        private static int ColumnNameToIndex(string columnName)
        {
            int columnIndex = 0;
            for (int i = 0; i < columnName.Length; i++)
            {
                columnIndex = (columnIndex * 26) + (columnName[i] - 'A' + 1);
            }
            return columnIndex - 1;
        }

        public static bool WriteUrkundeToExcel(string filePath, List<Gruppe> gruppen)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    IWorkbook workbook = new XSSFWorkbook(fs);
                    ISheet sheet = workbook.GetSheetAt(0);

                    int row = 2;
                    foreach (Gruppe gruppe in gruppen)
                    {
                        SetCellValue(sheet, "A", row, gruppe.GruppenName);
                        SetCellValue(sheet, "B", row, gruppe.Platz.ToString());
                        SetCellValue(sheet, "C", row, gruppe.GesamtPunkte.ToString());

                        row++;
                    }


                    using (FileStream writeFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(writeFileStream);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
        }

        public static bool WriteCheckUpToExcel(string filePath, Settings settings)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    IWorkbook workbook = new XSSFWorkbook(fs);
                    ISheet sheet = workbook.GetSheetAt(0);

                    SetCellValue(sheet, "A", 2, $"{settings.Veranstaltungstitel} am {settings.Veranstaltungsdatum:d} in {settings.Veranstaltungsort}");
                    SetCellValue(sheet, "A", 28, $"Zustimmung der Wettbewerbsleitung {settings.Veranstaltungsleitung}, durch Unterschrift:");

                    using (FileStream writeFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(writeFileStream);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
        }

        public static bool WritePlatzierungslisteToExcel(string filePath, List<Gruppe> gruppen)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    IWorkbook workbook = new XSSFWorkbook(fs);
                    ISheet sheet = workbook.GetSheetAt(0);

                    int row = 5;
                    foreach (Gruppe gruppe in gruppen)
                    {
                        SetCellValue(sheet, "A", row, gruppe.Feuerwehr);
                        SetCellValue(sheet, "B", row, gruppe.GruppenName);
                        SetCellValue(sheet, "C", row, gruppe.Organisationseinheit ?? "");
                        SetCellValue(sheet, "D", row, gruppe.Platz.ToString());
                        SetCellValue(sheet, "E", row, gruppe.GesamtPunkte.ToString());
                        SetCellValue(sheet, "F", row, gruppe.PunkteATeil.ToString());
                        SetCellValue(sheet, "G", row, gruppe.DurchschnittszeitKnotenATeil.ToString());
                        SetCellValue(sheet, "H", row, gruppe.PunkteBTeil.ToString());
                        SetCellValue(sheet, "I", row, gruppe.DurchschnittszeitBTeil.ToString());
                        SetCellValue(sheet, "J", row, gruppe.GesamtAlter.ToString());

                        row++;
                    }

                    using (FileStream writeFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(writeFileStream);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
        }

        public static bool WriteWertungsbogenToExcel(string filePath, List<Gruppe> gruppen, Settings settings)
        {
            try
            {
                List<string> excelPfade = new();
                string gesamtXlsxPfad = Path.Combine(filePath, $"Wertungsbogen-Gesamtübersicht.xlsx");
                string pdfPfad = Path.Combine(filePath, $"Wertungsbogen-Gesamtübersicht.pdf");

                foreach (Gruppe gruppe in gruppen)
                {
                    string excelpath = Path.Combine(filePath, $"Wertungsbogen-{gruppe.GruppennameOhneSonderzeichen}.xlsx");
                    excelPfade.Add(excelpath);
                    WriteFile.ByteArrayToFile(excelpath, BWB_Auswertung.Properties.Resources.Auswertungsbogen);

                    using (FileStream fs = new FileStream(excelpath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        IWorkbook workbook = new XSSFWorkbook(fs);
                        ISheet sheet = workbook.GetSheetAt(0);

                        //Gruppenname/Jugendfeuerwehr
                        SetCellValue(sheet, "C", 2, gruppe.GruppenName);
                        SetCellValue(sheet, "C", 3, gruppe.Organisationseinheit ?? "");
                        SetCellValue(sheet, "M", 2, gruppe.StartNr.ToString());
                        SetCellValue(sheet, "M", 3, gruppe.Platz.ToString());
                        SetCellValue(sheet, "A", 4, $"{settings.Veranstaltungstitel} am {settings.Veranstaltungsdatum:d} in {settings.Veranstaltungsort}");

                        //Eindrücke A Teil
                        SetCellValue(sheet, "G", 10, gruppe.EindruckGfMe.ToString());
                        SetCellValue(sheet, "G", 11, gruppe.EindruckMa.ToString());
                        SetCellValue(sheet, "G", 12, gruppe.EindruckA.ToString());
                        SetCellValue(sheet, "G", 13, gruppe.EindruckW.ToString());
                        SetCellValue(sheet, "G", 14, gruppe.EindruckS.ToString());

                        //Fehler A
                        SetCellValue(sheet, "J", 10, gruppe.FehlerGfMe.ToString());
                        SetCellValue(sheet, "J", 11, gruppe.FehlerMa.ToString());
                        SetCellValue(sheet, "J", 12, gruppe.FehlerA.ToString());
                        SetCellValue(sheet, "J", 13, gruppe.FehlerW.ToString());
                        SetCellValue(sheet, "J", 14, gruppe.FehlerS.ToString());
                        SetCellValue(sheet, "J", 15, gruppe.FehlerATeil.ToString());

                        //Eindruck B-Teil
                        SetCellValue(sheet, "G", 23, gruppe.EindruckLauefer1.ToString());
                        SetCellValue(sheet, "G", 24, gruppe.EindruckLauefer2.ToString());
                        SetCellValue(sheet, "G", 25, gruppe.EindruckLauefer3.ToString());
                        SetCellValue(sheet, "G", 26, gruppe.EindruckLauefer4.ToString());
                        SetCellValue(sheet, "G", 27, gruppe.EindruckLauefer5.ToString());
                        SetCellValue(sheet, "G", 28, gruppe.EindruckLauefer6.ToString());
                        SetCellValue(sheet, "G", 29, gruppe.EindruckLauefer7.ToString());
                        SetCellValue(sheet, "G", 30, gruppe.EindruckLauefer8.ToString());
                        SetCellValue(sheet, "G", 31, gruppe.EindruckLauefer9.ToString());

                        //Fehler B-Teil
                        SetCellValue(sheet, "J", 23, gruppe.FehlerLauefer1.ToString());
                        SetCellValue(sheet, "J", 24, gruppe.FehlerLauefer2.ToString());
                        SetCellValue(sheet, "J", 25, gruppe.FehlerLauefer3.ToString());
                        SetCellValue(sheet, "J", 26, gruppe.FehlerLauefer4.ToString());
                        SetCellValue(sheet, "J", 27, gruppe.FehlerLauefer5.ToString());
                        SetCellValue(sheet, "J", 28, gruppe.FehlerLauefer6.ToString());
                        SetCellValue(sheet, "J", 29, gruppe.FehlerLauefer7.ToString());
                        SetCellValue(sheet, "J", 30, gruppe.FehlerLauefer8.ToString());
                        SetCellValue(sheet, "J", 31, gruppe.FehlerLauefer9.ToString());
                        SetCellValue(sheet, "J", 32, gruppe.FehlerBTeil.ToString());
                        SetCellValue(sheet, "L", 32, gruppe.FehlerBTeil.ToString());

                        //Punkte und co
                        SetCellValue(sheet, "K", 38, gruppe.GesamtPunkte.ToString());
                        SetCellValue(sheet, "L", 19, gruppe.PunkteATeil.ToString());
                        SetCellValue(sheet, "L", 15, gruppe.PunkteATeil.ToString());

                        double differenzATeil = (Globals.SECONDS_ATEIL - gruppe.DurchschnittszeitATeil);
                        SetCellValue(sheet, "L", 17, differenzATeil <= 0 ? differenzATeil.ToString() : "0");

                        SetCellValue(sheet, "L", 19, gruppe.PunkteATeil.ToString());
                        SetCellValue(sheet, "L", 35, gruppe.PunkteBTeil.ToString());
                        SetCellValue(sheet, "N", 35, gruppe.PunkteATeil.ToString());
                        SetCellValue(sheet, "L", 18, gruppe.DurchschnittszeitKnotenATeil.ToString());
                        SetCellValue(sheet, "I", 18, gruppe.DurchschnittszeitKnotenATeil.ToString());
                        SetCellValue(sheet, "F", 18, gruppe.DurchschnittszeitKnotenATeil.ToString());
                        SetCellValue(sheet, "I", 16, gruppe.DurchschnittszeitATeil.ToString());
                        SetCellValue(sheet, "B", 17, settings.Vorgabezeit.ToString());

                        TimeSpan zeitateilspan = TimeSpan.FromSeconds(Convert.ToInt32(gruppe.DurchschnittszeitATeil));
                        SetCellValue(sheet, "D", 16, zeitateilspan.ToString(@"hh\:mm\:ss"));

                        SetCellValue(sheet, "G", 33, gruppe.SollZeitBTeilInSekunden.ToString());
                        SetCellValue(sheet, "G", 34, gruppe.DurchschnittszeitBTeil.ToString());

                        TimeSpan sollzeitbteilspan = TimeSpan.FromSeconds(Convert.ToInt32(gruppe.SollZeitBTeilInSekunden));
                        SetCellValue(sheet, "B", 33, sollzeitbteilspan.ToString(@"hh\:mm\:ss"));

                        TimeSpan zeitbteilspan = TimeSpan.FromSeconds(Convert.ToInt32(gruppe.DurchschnittszeitBTeil));
                        SetCellValue(sheet, "B", 34, zeitbteilspan.ToString(@"hh\:mm\:ss"));

                        SetCellValue(sheet, "K", 34, (gruppe.DurchschnittszeitBTeil - gruppe.SollZeitBTeilInSekunden).ToString());
                        SetCellValue(sheet, "M", 37, gruppe.Gesamteindruck.ToString());

                        using (FileStream writeFileStream = new FileStream(excelpath, FileMode.Create, FileAccess.Write))
                        {
                            workbook.Write(writeFileStream);
                        }
                    }
                }

                //Alle Excel Dateien zusätzlich in ein gebündeltes Workbook laden
                ExcelMerger merger = new ExcelMerger();
                merger.MergeExcelFiles(excelPfade, gesamtXlsxPfad);

                //Anschließende Konvertierung der Gesamt Excel Datei in eine PDF
                XlsxToPdfConverter converter = new XlsxToPdfConverter();
                converter.ConvertXlsxToPdf(gesamtXlsxPfad, pdfPfad);

                return true;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
        }
    }
}
