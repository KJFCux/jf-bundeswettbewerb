using BWB_Auswertung.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;


namespace BWB_Auswertung.IO
{
    public static class Excel
    {
        public static Gruppe ImportExcelGruppe(string path)
        {
            try
            {

                using (SpreadsheetDocument document = SpreadsheetDocument.Open(path, false))
                {
                    WorkbookPart workbookPart = document.WorkbookPart;
                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
                    List<Person> teilnehmende = new List<Person>();


                    string jugendfeuerwehr = Value(GetCell(sheetData, "C", 4), workbookPart).Trim();
                    string ou = Value(GetCell(sheetData, "C", 5), workbookPart).Trim();

                    for (uint i = 13; i <= 22; i++)
                    {
                        string nachname = Value(GetCell(sheetData, "B", i), workbookPart).Trim();
                        string vorname = Value(GetCell(sheetData, "C", i), workbookPart).Trim();
                        string tag = Value(GetCell(sheetData, "D", i), workbookPart);
                        string monat = Value(GetCell(sheetData, "E", i), workbookPart);
                        string jahr = Value(GetCell(sheetData, "F", i), workbookPart);

                        //Falls das Geburtsdatum nicht ausgefüllt ist als Standard den heutigen Tag nehmen
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

                string file = System.IO.Path.Combine(path, $"Gruppendaten.xlsx");
                WriteFile.ByteArrayToFile(file, BWB_Auswertung.Properties.Resources.Gruppendaten);

                using (SpreadsheetDocument document = SpreadsheetDocument.Open(file, true))
                {
                    WorkbookPart workbookPart = document.WorkbookPart;
                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                    uint index = 3;
                    foreach (Gruppe gruppe in gruppen)
                    {
                        foreach (Person teilnehmende in gruppe.Persons)
                        {
                            Cell cell = GetandCreateCell(sheetData, "A", index);
                            cell.CellValue = new CellValue(gruppe.Organisationseinheit ?? "");
                            cell.DataType = new EnumValue<CellValues>(CellValues.String);

                            cell = GetandCreateCell(sheetData, "B", index);
                            cell.CellValue = new CellValue(gruppe.Feuerwehr);
                            cell.DataType = new EnumValue<CellValues>(CellValues.String);

                            cell = GetandCreateCell(sheetData, "C", index);
                            cell.CellValue = new CellValue(gruppe.GruppenName);
                            cell.DataType = new EnumValue<CellValues>(CellValues.String);

                            cell = GetandCreateCell(sheetData, "D", index);
                            cell.CellValue = new CellValue(gruppe.StartNr.ToString() ?? "");
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                            cell = GetandCreateCell(sheetData, "E", index);
                            cell.CellValue = new CellValue(gruppe.Platz.ToString() ?? "");
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                            cell = GetandCreateCell(sheetData, "F", index);
                            cell.CellValue = new CellValue(gruppe.GesamtPunkte.ToString() ?? "");
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                            //Personen Daten
                            cell = GetandCreateCell(sheetData, "G", index);
                            cell.CellValue = new CellValue(teilnehmende.Geschlecht.ToString());
                            cell.DataType = new EnumValue<CellValues>(CellValues.String);

                            cell = GetandCreateCell(sheetData, "H", index);
                            cell.CellValue = new CellValue(teilnehmende.Vorname);
                            cell.DataType = new EnumValue<CellValues>(CellValues.String);

                            cell = GetandCreateCell(sheetData, "I", index);
                            cell.CellValue = new CellValue(teilnehmende.Nachname);
                            cell.DataType = new EnumValue<CellValues>(CellValues.String);

                            cell = GetandCreateCell(sheetData, "J", index);
                            cell.CellValue = new CellValue(teilnehmende.Geburtsdatum);
                            cell.DataType = new EnumValue<CellValues>(CellValues.Date);

                            cell = GetandCreateCell(sheetData, "K", index);
                            cell.CellValue = new CellValue(teilnehmende.Alter);
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                            index++;
                        }

                    }
                    worksheetPart.Worksheet.Save();
                    document.Dispose();
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        // Methode, um eine Zelle aus einem Arbeitsblatt zu erhalten
        private static Cell GetCell(SheetData sheetData, string columnName, uint rowIndex)
        {
            Row row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex);
            if (row != null)
            {
                return row.Elements<Cell>().FirstOrDefault(c => string.Compare(c.CellReference.Value, columnName + rowIndex, true) == 0);
            }
            return null;
        }
        public static SharedStringItem GetSharedStringItemById(WorkbookPart workbookPart, int id)
        {
            return workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(id);
        }
        private static string Value(Cell cell, WorkbookPart workbookPart)
        {
            string cellValue = string.Empty;
            if (cell != null)
            {
                if (cell.DataType != null && cell.DataType == CellValues.SharedString)
                {
                    int id = -1;

                    if (Int32.TryParse(cell.InnerText, out id))
                    {
                        SharedStringItem item = GetSharedStringItemById(workbookPart, id);

                        if (item.Text != null)
                        {
                            cellValue = item.Text.Text;
                        }
                        else if (item.InnerText != null)
                        {
                            cellValue = item.InnerText;
                        }
                        else if (item.InnerXml != null)
                        {
                            cellValue = item.InnerXml;
                        }
                    }
                }
                else
                {
                    cellValue = cell.InnerText;
                }
            }
            return cellValue;
        }

        public static bool WriteUrkundeToExcel(string filePath, List<Gruppe> gruppen)
        {
            try
            {
                using (SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, true))
                {
                    WorkbookPart workbookPart = document.WorkbookPart;
                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                    uint row = 2;
                    foreach (Gruppe gruppe in gruppen)
                    {
                        //Gruppenname/Jugendfeuerwehr
                        Cell cell = GetandCreateCell(sheetData, "A", row);
                        cell.CellValue = new CellValue(gruppe.GruppenName);
                        cell.DataType = new EnumValue<CellValues>(CellValues.String);

                        //Platz
                        cell = GetandCreateCell(sheetData, "B", row);
                        cell.CellValue = new CellValue(gruppe.Platz.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        //Punkte
                        cell = GetandCreateCell(sheetData, "C", row);
                        cell.CellValue = new CellValue(gruppe.GesamtPunkte.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        row++;
                    }

                    worksheetPart.Worksheet.Save();
                    document.Dispose();
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
                using (SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, true))
                {
                    WorkbookPart workbookPart = document.WorkbookPart;
                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                    //Titel der Veranstaltung
                    Cell cell = GetandCreateCell(sheetData, "A", 2);
                    cell.CellValue = new CellValue($"{settings.Veranstaltungstitel} am {settings.Veranstaltungsdatum.ToString("d")} in {settings.Veranstaltungsort}");
                    cell.DataType = new EnumValue<CellValues>(CellValues.String);

                    cell = GetandCreateCell(sheetData, "A", 28);
                    cell.CellValue = new CellValue($"Zustimmung der Wettbewerbsleitung {settings.Veranstaltungsleitung}, durch Unterschrift:");
                    cell.DataType = new EnumValue<CellValues>(CellValues.String);

                    worksheetPart.Worksheet.Save();
                    document.Dispose();
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
                using (SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, true))
                {
                    WorkbookPart workbookPart = document.WorkbookPart;
                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();


                    //Datum der Erstellung
                    //Cell cell = GetandCreateCell(sheetData, "I", 2);
                    //cell.CellValue = new CellValue(DateTime.Now.ToString("G"));
                    //cell.DataType = new EnumValue<CellValues>(CellValues.Date);

                    uint row = 5;

                    foreach (Gruppe gruppe in gruppen)
                    {
                        //Jugendfeuerwehr
                        Cell cell = GetandCreateCell(sheetData, "A", row);
                        cell.CellValue = new CellValue(gruppe.Feuerwehr);
                        cell.DataType = new EnumValue<CellValues>(CellValues.String);

                        //Gruppenname
                        cell = GetandCreateCell(sheetData, "B", row);
                        cell.CellValue = new CellValue(gruppe.GruppenName);
                        cell.DataType = new EnumValue<CellValues>(CellValues.String);

                        //Organisationseinheit
                        cell = GetandCreateCell(sheetData, "C", row);
                        cell.CellValue = new CellValue(gruppe.Organisationseinheit ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.String);

                        //Platz
                        cell = GetandCreateCell(sheetData, "D", row);
                        cell.CellValue = new CellValue(gruppe.Platz.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        //Gesamtpunkte
                        cell = GetandCreateCell(sheetData, "E", row);
                        cell.CellValue = new CellValue(gruppe.GesamtPunkte.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        //A-Teil
                        cell = GetandCreateCell(sheetData, "F", row);
                        cell.CellValue = new CellValue(gruppe.PunkteATeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        //Knoten
                        cell = GetandCreateCell(sheetData, "G", row);
                        cell.CellValue = new CellValue(gruppe.DurchschnittszeitKnotenATeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        //B-Teil
                        cell = GetandCreateCell(sheetData, "H", row);
                        cell.CellValue = new CellValue(gruppe.PunkteBTeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        //ZeitB-Teil
                        cell = GetandCreateCell(sheetData, "I", row);
                        cell.CellValue = new CellValue(gruppe.DurchschnittszeitBTeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        //Alter
                        cell = GetandCreateCell(sheetData, "J", row);
                        cell.CellValue = new CellValue(gruppe.GesamtAlter.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        row++;
                    }

                    worksheetPart.Worksheet.Save();
                    document.Dispose();
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


                foreach (Gruppe gruppe in gruppen)
                {
                    string excelpath = System.IO.Path.Combine(filePath, $"Wertungsbogen-{gruppe.GruppenName}.xlsx");
                    WriteFile.ByteArrayToFile(excelpath, BWB_Auswertung.Properties.Resources.Auswertungsbogen);
                    using (SpreadsheetDocument document = SpreadsheetDocument.Open(excelpath, true))
                    {
                        WorkbookPart workbookPart = document.WorkbookPart;
                        WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                        SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                        //Gruppenname/Jugendfeuerwehr
                        Cell cell = GetandCreateCell(sheetData, "C", 2);
                        cell.CellValue = new CellValue(gruppe.GruppenName);
                        cell.DataType = new EnumValue<CellValues>(CellValues.String);

                        cell = GetandCreateCell(sheetData, "C", 3);
                        cell.CellValue = new CellValue(gruppe.Organisationseinheit ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.String);


                        cell = GetandCreateCell(sheetData, "M", 2);
                        cell.CellValue = new CellValue(gruppe.StartNr.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "M", 3);
                        cell.CellValue = new CellValue(gruppe.Platz.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "A", 4);
                        cell.CellValue = new CellValue($"{settings.Veranstaltungstitel} am {settings.Veranstaltungsdatum.ToString("d")} in {settings.Veranstaltungsort}");
                        cell.DataType = new EnumValue<CellValues>(CellValues.String);

                        //Eindrücke A Teil
                        cell = GetandCreateCell(sheetData, "G", 10);
                        cell.CellValue = new CellValue(gruppe.EindruckGfMe.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "G", 11);
                        cell.CellValue = new CellValue(gruppe.EindruckMa.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "G", 12);
                        cell.CellValue = new CellValue(gruppe.EindruckA.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "G", 13);
                        cell.CellValue = new CellValue(gruppe.EindruckW.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "G", 14);
                        cell.CellValue = new CellValue(gruppe.EindruckS.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);


                        //Fehler A
                        cell = GetandCreateCell(sheetData, "J", 10);
                        cell.CellValue = new CellValue(gruppe.FehlerGfMe.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "J", 11);
                        cell.CellValue = new CellValue(gruppe.FehlerMa.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "J", 12);
                        cell.CellValue = new CellValue(gruppe.FehlerA.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "J", 13);
                        cell.CellValue = new CellValue(gruppe.FehlerW.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "J", 14);
                        cell.CellValue = new CellValue(gruppe.FehlerS.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "J", 15);
                        cell.CellValue = new CellValue(gruppe.FehlerATeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        //Eindruck B-Teil
                        cell = GetandCreateCell(sheetData, "G", 23);
                        cell.CellValue = new CellValue(gruppe.EindruckLauefer1.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "G", 24);
                        cell.CellValue = new CellValue(gruppe.EindruckLauefer2.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "G", 25);
                        cell.CellValue = new CellValue(gruppe.EindruckLauefer3.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "G", 26);
                        cell.CellValue = new CellValue(gruppe.EindruckLauefer4.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "G", 27);
                        cell.CellValue = new CellValue(gruppe.EindruckLauefer5.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "G", 28);
                        cell.CellValue = new CellValue(gruppe.EindruckLauefer6.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "G", 29);
                        cell.CellValue = new CellValue(gruppe.EindruckLauefer7.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "G", 30);
                        cell.CellValue = new CellValue(gruppe.EindruckLauefer8.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "G", 31);
                        cell.CellValue = new CellValue(gruppe.EindruckLauefer9.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        //Fehler B-Teil
                        cell = GetandCreateCell(sheetData, "J", 23);
                        cell.CellValue = new CellValue(gruppe.FehlerLauefer1.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "J", 24);
                        cell.CellValue = new CellValue(gruppe.FehlerLauefer2.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "J", 25);
                        cell.CellValue = new CellValue(gruppe.FehlerLauefer3.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "J", 26);
                        cell.CellValue = new CellValue(gruppe.FehlerLauefer4.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "J", 27);
                        cell.CellValue = new CellValue(gruppe.FehlerLauefer5.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "J", 28);
                        cell.CellValue = new CellValue(gruppe.FehlerLauefer6.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "J", 29);
                        cell.CellValue = new CellValue(gruppe.FehlerLauefer7.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "J", 30);
                        cell.CellValue = new CellValue(gruppe.FehlerLauefer8.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "J", 31);
                        cell.CellValue = new CellValue(gruppe.FehlerLauefer9.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "J", 32);
                        cell.CellValue = new CellValue(gruppe.FehlerBTeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        cell = GetandCreateCell(sheetData, "L", 32);
                        cell.CellValue = new CellValue(gruppe.FehlerBTeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        //Punkte und co
                        cell = GetandCreateCell(sheetData, "K", 38);
                        cell.CellValue = new CellValue(gruppe.GesamtPunkte.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "L", 19);
                        cell.CellValue = new CellValue(gruppe.PunkteATeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "L", 15);
                        cell.CellValue = new CellValue(gruppe.PunkteATeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        double differenzATeil = (Globals.SECONDS_ATEIL - gruppe.DurchschnittszeitATeil);
                        if (differenzATeil <= 0)
                        {
                            cell = GetandCreateCell(sheetData, "L", 17);
                            cell.CellValue = new CellValue(differenzATeil);
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        }
                        else
                        {
                            cell = GetandCreateCell(sheetData, "L", 17);
                            cell.CellValue = new CellValue("0");
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        }

                        cell = GetandCreateCell(sheetData, "M", 19);
                        cell.CellValue = new CellValue(gruppe.PunkteATeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "L", 35);
                        cell.CellValue = new CellValue(gruppe.PunkteBTeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "N", 35);
                        cell.CellValue = new CellValue(gruppe.PunkteATeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "L", 18);
                        cell.CellValue = new CellValue(gruppe.DurchschnittszeitKnotenATeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "I", 18);
                        cell.CellValue = new CellValue(gruppe.DurchschnittszeitKnotenATeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "F", 18);
                        cell.CellValue = new CellValue(gruppe.DurchschnittszeitKnotenATeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "I", 16);
                        cell.CellValue = new CellValue(gruppe.DurchschnittszeitATeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "B", 17);
                        cell.CellValue = new CellValue(settings.Vorgabezeit.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "D", 16);
                        TimeSpan zeitateilspan = new TimeSpan(0, 0, Convert.ToInt32(gruppe.DurchschnittszeitATeil));
                        cell.CellValue = new CellValue((zeitateilspan.Hours.ToString() + ":" + zeitateilspan.Minutes.ToString() + ":" + zeitateilspan.Seconds.ToString()) ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.String);

                        cell = GetandCreateCell(sheetData, "G", 33);
                        cell.CellValue = new CellValue(gruppe.SollZeitBTeilInSekunden.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "G", 34);
                        cell.CellValue = new CellValue(gruppe.DurchschnittszeitBTeil.ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "B", 33);
                        TimeSpan sollzeitbteilspan = new TimeSpan(0, 0, Convert.ToInt32(gruppe.SollZeitBTeilInSekunden));
                        cell.CellValue = new CellValue((sollzeitbteilspan.Hours.ToString() + ":" + sollzeitbteilspan.Minutes.ToString() + ":" + sollzeitbteilspan.Seconds.ToString()) ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.String);

                        cell = GetandCreateCell(sheetData, "B", 34);
                        TimeSpan zeitbteilspan = new TimeSpan(0, 0, Convert.ToInt32(gruppe.DurchschnittszeitBTeil));
                        cell.CellValue = new CellValue((zeitbteilspan.Hours.ToString() + ":" + zeitbteilspan.Minutes.ToString() + ":" + zeitbteilspan.Seconds.ToString()) ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.String);

                        cell = GetandCreateCell(sheetData, "K", 34);
                        cell.CellValue = new CellValue((gruppe.DurchschnittszeitBTeil - gruppe.SollZeitBTeilInSekunden).ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        cell = GetandCreateCell(sheetData, "M", 37);
                        cell.CellValue = new CellValue((gruppe.Gesamteindruck).ToString() ?? "");
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        worksheetPart.Worksheet.Save();
                        document.Dispose();
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

        private static Cell GetandCreateCell(SheetData sheetData, string columnName, uint rowIndex)
        {
            Row row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex);
            if (row == null)
            {
                // Zeile erstellen, wenn sie nicht vorhanden ist
                row = new Row() { RowIndex = rowIndex };
                sheetData.Append(row);
            }

            string cellReference = columnName + rowIndex;
            Cell cell = row.Elements<Cell>().FirstOrDefault(c => c.CellReference.Value == cellReference);
            if (cell == null)
            {
                // Zelle erstellen, wenn sie nicht vorhanden ist
                cell = new Cell() { CellReference = cellReference };
                row.InsertAt(cell, GetInsertIndex(row, cellReference));
            }

            return cell;
        }
        private static int GetInsertIndex(Row row, string cellReference)
        {
            int insertIndex = 0;
            foreach (Cell cell in row.Elements<Cell>())
            {
                if (string.Compare(cell.CellReference.Value, cellReference, true) > 0)
                {
                    break;
                }
                insertIndex++;
            }
            return insertIndex;
        }
        private static Row GetRow(Worksheet worksheet, uint rowIndex)
        {
            return worksheet.GetFirstChild<SheetData>().Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex);
        }
    }
}
