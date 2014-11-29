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
        public static class IrcConstants
        {
            public const char CtcpChar = '\x1';
            public const char IrcBold = '\x2';
            public const char IrcColor = '\x3';
            public const char IrcReverse = '\x16';
            public const char IrcNormal = '\xf';
            public const char IrcUnderline = '\x1f';
            public const char CtcpQuoteChar = '\x20';

        }

        public enum IrcColors
        {
            White = 0,
            Black = 1,
            Blue = 2,
            Green = 3,
            LightRed = 4,
            Brown = 5,
            Purple = 6,
            Orange = 7,
            Yellow = 8,
            LightGreen = 9,
            Cyan = 10,
            LightCyan = 11,
            LightBlue = 12,
            Pink = 13,
            Grey = 14,
            LightGrey = 15,
            Transparent = 99
        }

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
                Log(ex.Message);
            }
        }

        public static object LoadObject(string filename)
        {
            using (Stream stream = File.Open(filename, FileMode.Open))
            {
                return new BinaryFormatter().Deserialize(stream);
            }
        }

        public static string DownloadWebPage(string url)
        {
            return DownloadWebPage(url, false);
        }

        public static string DownloadWebPage(string url, bool identifyAsBrowser)
        {
            string response = String.Empty;
            var oldCallback = ServicePointManager.ServerCertificateValidationCallback;
            try
            {
                using (WebClient w = new WebClient())
                {
                    ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
                    w.Encoding = Encoding.UTF8;
                    if(identifyAsBrowser)
                        w.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                    response = w.DownloadString(url);
                }
            }
            catch (Exception e)
            {
                Log(e.Message, e.StackTrace);
            }
            ServicePointManager.ServerCertificateValidationCallback = oldCallback;
            return response;
        }

        public static string CreateShortUrl(string longurl)
        {
            using (WebClient web = new WebClient())
            {
                var oldCallback = ServicePointManager.ServerCertificateValidationCallback;
                try
                {
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
                    Log(e.Message);
                    Log(e.StackTrace);
                }
                ServicePointManager.ServerCertificateValidationCallback = oldCallback;
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

        public static void Log(params string[] lines)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("error.log", true, Encoding.UTF8))
            {
                List<string> split = new List<string>();
                lines.ToList().ForEach(l => split.AddRange(l.Split(Environment.NewLine.ToCharArray())));

                foreach (var line in split.Where(s => !string.IsNullOrWhiteSpace(s)))
                    file.WriteLine(string.Format("{0:s}: {1}", DateTime.Now, line));
            }
        }
    }
}
