using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using LagerInsights.Models;
using LagerInsights.Properties;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace LagerInsights.IO;

public static class Excel
{
    public static void ExportExcelGruppen(List<Jugendfeuerwehr> gruppen, string path, MainViewModel viewModel)
    {
        try
        {
            var file = Path.Combine(path, "Gruppendaten.xlsx");
            WriteFile.ByteArrayToFile(file, Resources.Gruppendaten);

            using (var fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
            {
                IWorkbook workbook = new XSSFWorkbook(fs);
                var sheet = workbook.GetSheetAt(0);



                SetCellValue(sheet, "B", 2, viewModel.alleTeilnehmenden().Count.ToString());
                SetCellValue(sheet, "B", 4, viewModel.AnzahlUnvertraeglichkeiten().ToString());
                SetCellValue(sheet, "F", 4, viewModel.AnzahlVegetarisch().ToString());
                SetCellValue(sheet, "I", 4, viewModel.AnzahlVegan().ToString());

                var index = 10;

                foreach (var gruppe in gruppen)
                    foreach (var teilnehmende in gruppe.Persons)
                    {
                        string unterlagen = gruppe.Einverstaendniserklaerung.HasValue && gruppe.Einverstaendniserklaerung.Value ? "Ja" : "Nein";

                        SetCellValue(sheet, "A", index, teilnehmende.Nachname);
                        SetCellValue(sheet, "B", index, teilnehmende.Vorname);
                        SetCellValue(sheet, "C", index, teilnehmende.Geburtsdatum.ToString("dd.MM.yyyy"));
                        SetCellValue(sheet, "D", index, teilnehmende.Alter.ToString());
                        SetCellValue(sheet, "E", index, teilnehmende.Geschlecht.ToString());
                        SetCellValue(sheet, "F", index, teilnehmende.Plz ?? "");
                        SetCellValue(sheet, "G", index, teilnehmende.Ort ?? "");
                        SetCellValue(sheet, "H", index, teilnehmende.Strasse ?? "");
                        SetCellValue(sheet, "I", index, teilnehmende.StatusFriendlyName);
                        SetCellValue(sheet, "J", index, gruppe.Feuerwehr);
                        SetCellValue(sheet, "K", index, gruppe.Organisationseinheit ?? "");
                        SetCellValue(sheet, "L", index, teilnehmende.Essgewohnheiten);
                        SetCellValue(sheet, "M", index, teilnehmende.Unvertraeglichkeiten);
                        SetCellValue(sheet, "N", index, gruppe.ZuBezahlenderBetrag.ToString("C"));
                        SetCellValue(sheet, "O", index, gruppe.GezahlterBeitrag?.ToString("C") ?? "");
                        SetCellValue(sheet, "P", index, unterlagen);
                        SetCellValue(sheet, "Q", index, gruppe.Zeltdorf ?? "Nicht zugewiesen");

                        index++;
                    }

                using (var writeFileStream = new FileStream(file, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(writeFileStream);
                }
            }
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }

    private static void SetCellValue(ISheet sheet, string columnName, int rowIndex, string value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(ColumnNameToIndex(columnName)) ?? row.CreateCell(ColumnNameToIndex(columnName));
        cell.SetCellValue(value);
    }

    private static string GetCellValue(ISheet sheet, string columnName, int rowIndex)
    {
        var row = sheet.GetRow(rowIndex);
        var cell = row?.GetCell(ColumnNameToIndex(columnName));
        return cell?.ToString() ?? string.Empty;
    }

    private static int ColumnNameToIndex(string columnName)
    {
        var columnIndex = 0;
        for (var i = 0; i < columnName.Length; i++) columnIndex = columnIndex * 26 + (columnName[i] - 'A') + 1;
        return columnIndex - 1;
    }

}