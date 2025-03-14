using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

public class XlsxToPdfConverter
{
    public void ConvertXlsxToPdf(string xlsxPath, string pdfPath)
    {
        // Laden der XLSX-Datei
        IWorkbook workbook;
        using (var fileStream = new FileStream(xlsxPath, FileMode.Open, FileAccess.Read))
        {
            workbook = new XSSFWorkbook(fileStream);
        }

        // Erstellen der PDF-Datei
        using (var pdfFileStream = new FileStream(pdfPath, FileMode.Create, FileAccess.Write))
        {
            var writer = new PdfWriter(pdfFileStream);
            var pdfDocument = new PdfDocument(writer);
            var document = new Document(pdfDocument);

            // Durch alle Arbeitsblätter gehen
            for (var i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);

                // Durch alle Zeilen gehen
                for (var rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row != null)
                        // Durch alle Zellen gehen
                        for (var colIndex = 0; colIndex < row.LastCellNum; colIndex++)
                        {
                            var cell = row.GetCell(colIndex);
                            if (cell != null)
                            {
                                var cellValue = GetCellValue(cell);
                                var paragraph = new Paragraph(cellValue);
                                paragraph.SetTextAlignment(TextAlignment.LEFT);
                                document.Add(paragraph);
                            }
                        }
                }

                // Seite hinzufügen, wenn es noch weitere Arbeitsblätter gibt
                if (i < workbook.NumberOfSheets - 1) document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
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
                if (DateUtil.IsCellDateFormatted(cell)) return cell.DateCellValue?.ToString("dd/MM/yyyy") ?? "";

                return cell.NumericCellValue.ToString();
            case CellType.Boolean:
                return cell.BooleanCellValue.ToString();
            case CellType.Formula:
                return cell.CellFormula;
            default:
                return string.Empty;
        }
    }
}