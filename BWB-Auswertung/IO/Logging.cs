using System;
using System.Diagnostics;
using System.IO;

namespace BWB_Auswertung.IO
{
    public static class LOGGING
    {

        public static void Write(string message, string method, EventLogEntryType entryType = EventLogEntryType.Information)
        {
            WriteLog(message, method, entryType);
            WriteEvent(message, method, entryType);

        }

        private static void WriteLog(string message, string method,
            EventLogEntryType entryType = EventLogEntryType.Information)
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), System.AppDomain.CurrentDomain.FriendlyName, "log");
                StreamWriter sw = new StreamWriter(Path.Combine(path, $"{method}.log"), true);
                sw.WriteLine($"{DateTime.Now.ToString()} : {entryType.ToString()} : {message}");
                sw.Flush();
                sw.Close();
            }
            catch (Exception ex)
            {
                WriteEvent(message, method, entryType);
            }
        }

        private static void WriteEvent(string message, string method,
            EventLogEntryType entryType = EventLogEntryType.Information)
        {
            try
            {
                EventLog.WriteEntry(System.AppDomain.CurrentDomain.FriendlyName,
                    $"{DateTime.Now.ToString()} : {method} : {entryType.ToString()} : {message}", entryType, (int)entryType);
            }
            catch (Exception ex)
            {
                //No Permission to Create Eventlog Source
                EventLog.WriteEntry("Application",
                    $"{DateTime.Now.ToString()} : {method} : {entryType.ToString()} : {ex} & {message}", entryType, (int)entryType);
            }
        }

    }
}
