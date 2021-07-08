using Newtonsoft.Json;

public class JsonHelper
{
    /// <summary>
    /// Json序列化数据
    /// </summary>
    public static string Serialize<T>(T data)
    {
        string result = string.Empty;

        try
        {
            result = JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        catch
        {

        }

        return result;
    }

    /// <summary>
    /// Json反序列化数据
    /// </summary>
    public static T DeSerialize<T>(string data)
    {
        try
        {
            return (T)JsonConvert.DeserializeObject(data, typeof(T));
        }
        catch
        {

        }

        return default(T);
    }

    /// <summary>
    /// Json反序列化数据
    /// </summary>
    public static T DeSerialize<T>(string data, bool ignoreNullValue = true)
    {
        if (ignoreNullValue)
        {
            try
            {
                return (T)JsonConvert.DeserializeObject(data, typeof(T), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            catch
            {

            }
        }

        return DeSerialize<T>(data);
    }

    /// <summary>
    /// XML转Json
    /// </summary>
    /// <param name="xml"></param>
    /// <returns></returns>
    public static string XmlToJson(string xml)
    {
        System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

        doc.LoadXml(xml);

        string json = JsonConvert.SerializeXmlNode(doc);

        return json;
    }
}
