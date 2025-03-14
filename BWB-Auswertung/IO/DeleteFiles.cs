using System;
using System.Collections.Generic;
using System.Linq;

namespace BWB_Auswertung.IO
{
    public static class DeleteFiles
    {
        public static void DeleteFilesExcept(List<string> excludes, string ordnerPfad)
        {
            try
            {
                var files = System.IO.Directory.GetFiles(ordnerPfad).Where(x => !excludes.Contains(System.IO.Path.GetFileName(x)));
                foreach (var file in files)
                {
                    System.IO.File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }
    }
}
