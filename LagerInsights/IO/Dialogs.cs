using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace LagerInsights.IO;

public class Dialogs
{
    public string ShowFolderBrowserDialog()
    {
        try
        {
            var selectedFolderPath = string.Empty;

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Ordner auswählen oder erstellen";
                dialog.RootFolder = Environment.SpecialFolder.Desktop;
                dialog.ShowNewFolderButton = true;

                var result = dialog.ShowDialog();

                if (result == DialogResult.OK) selectedFolderPath = dialog.SelectedPath;
            }

            return selectedFolderPath;
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
            return string.Empty;
        }
    }
}