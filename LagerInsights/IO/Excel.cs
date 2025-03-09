using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LagerInsights.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace LagerInsights.IO
{
    public static class Excel
    {

        public static void ExportExcelGruppen(List<Jugendfeuerwehr> gruppen, string path)
        {
            try
            {
                string file = Path.Combine(path, $"Gruppendaten.xlsx");
                WriteFile.ByteArrayToFile(file, LagerInsights.Properties.Resources.Gruppendaten);

                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
                {
                    IWorkbook workbook = new XSSFWorkbook(fs);
                    ISheet sheet = workbook.GetSheetAt(0);

                    int index = 3;
                    foreach (Jugendfeuerwehr gruppe in gruppen)
                    {
                        foreach (Person teilnehmende in gruppe.Persons)
                        {
                            SetCellValue(sheet, "A", index, gruppe.Organisationseinheit ?? "");
                            SetCellValue(sheet, "B", index, gruppe.Feuerwehr);
                            SetCellValue(sheet, "C", index, gruppe.LagerNr.ToString() ?? "");
                            SetCellValue(sheet, "D", index, teilnehmende.Geschlecht.ToString());
                            SetCellValue(sheet, "E", index, teilnehmende.Vorname);
                            SetCellValue(sheet, "F", index, teilnehmende.Nachname);
                            SetCellValue(sheet, "G", index, teilnehmende.Geburtsdatum.ToString("yyyy-MM-dd"));
                            SetCellValue(sheet, "H", index, teilnehmende.Alter.ToString());
                            SetCellValue(sheet, "I", index, teilnehmende.StatusFriendlyName);
                            SetCellValue(sheet, "J", index, teilnehmende.Essgewohnheiten);
                            SetCellValue(sheet, "K", index, teilnehmende.Unvertraeglichkeiten);
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

    }
}
