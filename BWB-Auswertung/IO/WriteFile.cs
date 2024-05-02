using System;
using System.IO;
using System.Text;

namespace BWB_Auswertung.IO
{
    public static class WriteFile
    {
        /* public async Task WriteTextAsync(string filePath, string text)
         {
             byte[] encodedText = Encoding.Unicode.GetBytes(text);

             using (FileStream sourceStream = new FileStream(filePath,
                        FileMode.Truncate, FileAccess.Write, FileShare.None,
                        bufferSize: 4096, useAsync: true))
             {
                 await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
             };
         }*/

        public static void writeText(string filepath, string text)
        {
            try
            {
                StreamWriter sw = new StreamWriter(filepath, false, Encoding.Unicode);
                sw.Write(text);
                sw.Close();
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
            }
        }
        public static bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
        }
    }
}
