using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
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

        public static string CreateShortUrl(string longurl)
        {
            using (WebClient web = new WebClient())
            {
                try
                {
                    var oldCallback = ServicePointManager.ServerCertificateValidationCallback;
                    ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
                    string postdata = "{\"longUrl\": \"" + longurl + "\"}";
                    web.Encoding = Encoding.UTF8;
                    web.Headers.Add("Content-Type", "application/json");
                    byte[] dataresp = web.UploadData("https://www.googleapis.com/urlshortener/v1/url", "POST", Encoding.UTF8.GetBytes(postdata));
                    GoogleShort response = new JavaScriptSerializer().Deserialize<GoogleShort>(web.Encoding.GetString(dataresp));
                    ServicePointManager.ServerCertificateValidationCallback = oldCallback;
                    return response.id;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
            return String.Empty;
        }

        class GoogleShort
        {
            public string kind;
            public string id;
            public string longUrl;
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true; //lol security
        }
    }
}
