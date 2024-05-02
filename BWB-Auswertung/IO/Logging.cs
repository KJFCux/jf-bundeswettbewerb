using System;
using System.Diagnostics;
using System.IO;

namespace BWB_Auswertung.IO
{
    public static class LOGGING
    {

        public static void Write(string message, string method, EventLogEntryType entryType = EventLogEntryType.Information)
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), System.AppDomain.CurrentDomain.FriendlyName, "log");
                StreamWriter sw = new StreamWriter(Path.Combine(path, $"{method}.log"), true);
                sw.WriteLine($"{DateTime.Now.ToString()} : {entryType.ToString()} : {message}");
                sw.Flush();
                sw.Close();
                EventLog.WriteEntry(System.AppDomain.CurrentDomain.FriendlyName,
                    $"{DateTime.Now.ToString()} : {method} : {entryType.ToString()} : {message}", entryType, (int)entryType);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(System.AppDomain.CurrentDomain.FriendlyName,
                    $"{DateTime.Now.ToString()} : {method} : {entryType.ToString()} : {ex} & {message}", entryType, (int)entryType);
            }
        }
    }
}
