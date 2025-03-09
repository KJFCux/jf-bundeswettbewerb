using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

public class ExcelMerger
{
    public void MergeExcelFiles(List<string> filePaths, string outputFilePath)
    {
        IWorkbook outputWorkbook = new XSSFWorkbook();

        foreach (var filePath in filePaths)
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook inputWorkbook = new XSSFWorkbook(fileStream);
                for (var i = 0; i < inputWorkbook.NumberOfSheets; i++)
                {
                    var inputSheet = inputWorkbook.GetSheetAt(i);
                    var sheetName = Path.GetFileNameWithoutExtension(filePath) + "_" + inputSheet.SheetName;
                    var outputSheet = outputWorkbook.CreateSheet(sheetName);

                    CopySheet(inputSheet, outputSheet);
                }
            }

        using (var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
        {
            outputWorkbook.Write(fileStream);
        }
    }

    private void CopySheet(ISheet inputSheet, ISheet outputSheet)
    {
        for (var i = 0; i <= inputSheet.LastRowNum; i++)
        {
            var inputRow = inputSheet.GetRow(i);
            if (inputRow != null)
            {
                var outputRow = outputSheet.CreateRow(i);
                CopyRow(inputRow, outputRow);
            }
        }
    }

    private void CopyRow(IRow inputRow, IRow outputRow)
    {
        for (var i = 0; i < inputRow.LastCellNum; i++)
        {
            var inputCell = inputRow.GetCell(i);
            if (inputCell != null)
            {
                var outputCell = outputRow.CreateCell(i);
                CopyCell(inputCell, outputCell);
            }
        }
    }

    private void CopyCell(ICell inputCell, ICell outputCell)
    {
        outputCell.SetCellType(inputCell.CellType);

        switch (inputCell.CellType)
        {
            case CellType.String:
                outputCell.SetCellValue(inputCell.StringCellValue);
                break;
            case CellType.Numeric:
                outputCell.SetCellValue(inputCell.NumericCellValue);
                break;
            case CellType.Boolean:
                outputCell.SetCellValue(inputCell.BooleanCellValue);
                break;
            case CellType.Formula:
                outputCell.SetCellFormula(inputCell.CellFormula);
                break;
        }
    }
}