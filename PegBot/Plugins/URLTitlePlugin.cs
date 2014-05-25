using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PegBot.Plugins
{
    class URLTitlePlugin : BotPlugin
    {
        public URLTitlePlugin(IrcClient irc)
            : base(irc, "URLTitle")
        {
            irc.OnChannelMessage += new IrcEventHandler(OnChannelMessage);
        }

        public void OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (ChannelEnabled(e.Data.Channel))
            {
                if (Uri.IsWellFormedUriString(e.Data.Message, UriKind.RelativeOrAbsolute))
                {
                    try
                    {
                        string title = GetWebPageTitle(e.Data.Message);
                        if (!string.IsNullOrEmpty(title))
                            irc.SendMessage(SendType.Message, e.Data.Channel, title);
                    }
                    catch
                    {
                        //don't bother
                    }
                }
            }
        }

        public string GetWebPageTitle(string url)
        {
            var oldCallback = ServicePointManager.ServerCertificateValidationCallback;
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = PluginUtils.ValidateServerCertificate;

                HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;

                if (request == null) return null;

                request.UseDefaultCredentials = true;

                HttpWebResponse response = null;
                try { response = request.GetResponse() as HttpWebResponse; }
                catch (WebException) { return null; }

                string regex = @"(?<=<title.*>)([\s\S]*)(?=</title>)";

                if (new List<string>(response.Headers.AllKeys).Contains("Content-Type"))
                    if (response.Headers["Content-Type"].StartsWith("text/html"))
                    {
                        // Download the page
                        string page;
                        using (WebClient web = new WebClient())
                        {
                            web.UseDefaultCredentials = true;
                            web.Encoding = Encoding.UTF8;
                            page = web.DownloadString(url);
                        }
                        // Extract the title
                        Regex ex = new Regex(regex, RegexOptions.IgnoreCase);
                        return ex.Match(page).Value.Trim();
                    }
            }
            catch { }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = oldCallback;
            }
            return null;
        }
    }
}
