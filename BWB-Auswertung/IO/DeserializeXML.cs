using System;
using System.IO;
using System.Xml.Serialization;

namespace BWB_Auswertung.IO
{
    public class DeserializeXML<T> where T : class
    {
        public static T Deserialize<T>(string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    return (T)serializer.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {

                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);

                return default(T);
            }
        }
    }
}
