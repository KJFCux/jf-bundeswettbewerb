using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LagerInsights.IO;

public static class DeleteFiles
{
    public static void DeleteFilesExcept(List<string> excludes, string ordnerPfad)
    {
        try
        {
            var files = Directory.GetFiles(ordnerPfad).Where(x => !excludes.Contains(Path.GetFileName(x)));
            foreach (var file in files) File.Delete(file);
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);
        }
    }
}