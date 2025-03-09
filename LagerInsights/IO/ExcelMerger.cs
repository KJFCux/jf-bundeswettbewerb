using System;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

public class ExcelMerger
{
    public void MergeExcelFiles(List<string> filePaths, string outputFilePath)
    {
        IWorkbook outputWorkbook = new XSSFWorkbook();

        foreach (string filePath in filePaths)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook inputWorkbook = new XSSFWorkbook(fileStream);
                for (int i = 0; i < inputWorkbook.NumberOfSheets; i++)
                {
                    ISheet inputSheet = inputWorkbook.GetSheetAt(i);
                    string sheetName = Path.GetFileNameWithoutExtension(filePath) + "_" + inputSheet.SheetName;
                    ISheet outputSheet = outputWorkbook.CreateSheet(sheetName);

                    CopySheet(inputSheet, outputSheet);
                }
            }
        }

        using (FileStream fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
        {
            outputWorkbook.Write(fileStream);
        }
    }

    private void CopySheet(ISheet inputSheet, ISheet outputSheet)
    {
        for (int i = 0; i <= inputSheet.LastRowNum; i++)
        {
            IRow inputRow = inputSheet.GetRow(i);
            if (inputRow != null)
            {
                IRow outputRow = outputSheet.CreateRow(i);
                CopyRow(inputRow, outputRow);
            }
        }
    }

    private void CopyRow(IRow inputRow, IRow outputRow)
    {
        for (int i = 0; i < inputRow.LastCellNum; i++)
        {
            ICell inputCell = inputRow.GetCell(i);
            if (inputCell != null)
            {
                ICell outputCell = outputRow.CreateCell(i);
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
            default:
                break;
        }
    }
}
