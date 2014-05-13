using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace PegBot
{
    class PluginUtils
    {
        public static void SaveObject(object o, string filename)
        {
            try
            {
                using (Stream stream = File.Open(filename, FileMode.Create))
                {
                    new BinaryFormatter().Serialize(stream, o);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static object LoadObject(string filename)
        {
            using (Stream stream = File.Open(filename, FileMode.Open))
            {
                return new BinaryFormatter().Deserialize(stream);
            }
        }

        public static string GetShortUrl(string url)
        {
            using (WebClient web = new WebClient())
            {
                try
                {
                    string postdata = "{\"longUrl\": \"" + url + "\"}";
                    web.Encoding = Encoding.UTF8;
                    web.Headers.Add("Content-Type", "application/json");
                    byte[] dataresp = web.UploadData("https://www.googleapis.com/urlshortener/v1/url", "POST", Encoding.UTF8.GetBytes(postdata));
                    GoogleShort response = new JavaScriptSerializer().Deserialize<GoogleShort>(web.Encoding.GetString(dataresp));
                    return response.id;
                }
                catch (Exception) { }
            }
            return null;
        }

        class GoogleShort
        {
            public string kind;
            public string id;
            public string longUrl;
        }
    }
}
