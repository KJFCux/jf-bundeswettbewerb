using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace BWB_Auswertung.IO
{
    public class SerializeXML<T> where T : class
    {
        public static string Serialize<T>(T data)
        {
            try
            {
                XmlSerializer xsSubmit = new XmlSerializer(typeof(T));
                using (var sww = new StringWriter())
                {
                    using (XmlTextWriter writer = new XmlTextWriter(sww) { Formatting = Formatting.Indented })
                    {
                        xsSubmit.Serialize(writer, data);
                        return sww.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, System.Diagnostics.EventLogEntryType.Error);
                return string.Empty;
            }
        }

    }
}
