using System;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

public class XlsxToPdfConverter
{
    public void ConvertXlsxToPdf(string xlsxPath, string pdfPath)
    {
        // Laden der XLSX-Datei
        IWorkbook workbook;
        using (FileStream fileStream = new FileStream(xlsxPath, FileMode.Open, FileAccess.Read))
        {
            workbook = new XSSFWorkbook(fileStream);
        }

        // Erstellen der PDF-Datei
        using (FileStream pdfFileStream = new FileStream(pdfPath, FileMode.Create, FileAccess.Write))
        {
            PdfWriter writer = new PdfWriter(pdfFileStream);
            PdfDocument pdfDocument = new PdfDocument(writer);
            Document document = new Document(pdfDocument);

            // Durch alle Arbeitsblätter gehen
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                ISheet sheet = workbook.GetSheetAt(i);

                // Durch alle Zeilen gehen
                for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    IRow row = sheet.GetRow(rowIndex);
                    if (row != null)
                    {
                        // Durch alle Zellen gehen
                        for (int colIndex = 0; colIndex < row.LastCellNum; colIndex++)
                        {
                            ICell cell = row.GetCell(colIndex);
                            if (cell != null)
                            {
                                string cellValue = GetCellValue(cell);
                                Paragraph paragraph = new Paragraph(cellValue);
                                paragraph.SetTextAlignment(TextAlignment.LEFT);
                                document.Add(paragraph);
                            }
                        }
                    }
                }

                // Seite hinzufügen, wenn es noch weitere Arbeitsblätter gibt
                if (i < workbook.NumberOfSheets - 1)
                {
                    document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }
            }

            document.Close();
        }
    }

    private string GetCellValue(ICell cell)
    {
        switch (cell.CellType)
        {
            case CellType.String:
                return cell.StringCellValue;
            case CellType.Numeric:
                if (DateUtil.IsCellDateFormatted(cell))
                {
                    return cell.DateCellValue?.ToString("dd/MM/yyyy") ?? "";
                }
                else
                {
                    return cell.NumericCellValue.ToString();
                }
            case CellType.Boolean:
                return cell.BooleanCellValue.ToString();
            case CellType.Formula:
                return cell.CellFormula;
            default:
                return string.Empty;
        }
    }
}
