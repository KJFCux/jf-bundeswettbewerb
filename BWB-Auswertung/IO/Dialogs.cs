using System;
using System.Windows.Forms;

namespace BWB_Auswertung.IO
{
    public class Dialogs
    {
        public string ShowFolderBrowserDialog()
        {
            try
            {
                string selectedFolderPath = string.Empty;

                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Ordner auswählen oder erstellen";
                    dialog.RootFolder = Environment.SpecialFolder.Desktop;
                    dialog.ShowNewFolderButton = true;

                    DialogResult result = dialog.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        selectedFolderPath = dialog.SelectedPath;
                    }
                }

                return selectedFolderPath;
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                return string.Empty;
            }
        }
    }
}
