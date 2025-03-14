using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace LagerInsights.IO;

public class DeserializeXML<T> where T : class
{
    public static T Deserialize<T>(string filePath)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var fs = new FileStream(filePath, FileMode.Open))
            {
                return (T)serializer.Deserialize(fs);
            }
        }
        catch (Exception ex)
        {
            LOGGING.Write(ex.Message, MethodBase.GetCurrentMethod().Name, EventLogEntryType.Error);

            return default;
        }
    }
}