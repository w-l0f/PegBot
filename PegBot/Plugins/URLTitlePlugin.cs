using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace PegBot.Plugins
{
    class URLTitlePlugin : BotPlugin
    {
        private static object urlLock = new object();
        public URLTitlePlugin(IrcClient irc)
            : base(irc, "URLTitle")
        {
            irc.OnChannelMessage += new IrcEventHandler(OnChannelMessage);
        }

        public void OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (ChannelEnabled(e.Data.Channel))
            {
                foreach (string s in e.Data.Message.Split(' '))
                    if (Uri.IsWellFormedUriString(s, UriKind.Absolute))
                    {
                        try
                        {
                            string title = GetWebPageTitle(s);
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
            lock (urlLock)
            {
                var oldCallback = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = PluginUtils.ValidateServerCertificate;
                try
                {
                    using (WebClient web = new WebClient())
                    {
                        string site = web.DownloadString(url);
                        string title = Regex.Match(site, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
                        if (!string.IsNullOrEmpty(title))
                            return HttpUtility.HtmlDecode(title);
                    }
                }
                catch { }
                finally
                {
                    ServicePointManager.ServerCertificateValidationCallback = oldCallback;
                }
            }
            return null;
        }
    }
}