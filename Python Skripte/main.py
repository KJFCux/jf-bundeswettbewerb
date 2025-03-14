# This is a sample Python script.

# Press Umschalt+F10 to execute it or replace it with your code.
# Press Double Shift to search everywhere for classes, files, tool windows, actions, and settings.


from pathlib import Path
import win32com.client as win32
import os


def convert_xls_to_xlsx(path: Path) -> None:
    excel = win32.gencache.EnsureDispatch('Excel.Application')
    wb = excel.Workbooks.Open(path.absolute())

    # FileFormat=51 is for .xlsx extension
    wb.SaveAs(str(path.absolute().with_suffix(".xlsx")), FileFormat=51)
    wb.Close()
    excel.Application.Quit()


# Press the green button in the gutter to run the script.
if __name__ == '__main__':

    for root, dirs, files in os.walk(
            r'C:\Users\maximilian.janzen\Downloads\Erfolgreich\Erfolgreich'):
        # select file name
        for file in files:
            # check the extension of files
            if file.endswith('.xls'):
                convert_xls_to_xlsx(Path(os.path.join(root, file)))

# See PyCharm help at https://www.jetbrains.com/help/pycharm/
