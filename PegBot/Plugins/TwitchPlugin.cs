using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace PegBot.Plugins
{
    class TwitchPlugin : BotPlugin
    {
        private List<string> OnlineChannels = new List<string>();
        private Dictionary<string, string> Urls = new Dictionary<string, string>();
        private Timer TwitchTimer;
        private const int UpdateIntervalMinutes = 2;

        public TwitchPlugin(IrcClient irc)
            : base(irc, "Twitch")
        {
            RegisterCommand(".twitch list", "List all currently subscribed Twitch channels", OnListTwitchChannels, false);
            RegisterCommand(".twitch add", "<Twitch channel>", "Add <Twitch channel> to subscription list", OnAddTwitchChannel);
            RegisterCommand(".twitch remove", "<Twitch channel>", "Remove <Twitch channel> from subscription list", OnRemoveTwitchChannel);

            //add all channels as online in order no to spam
            OnlineChannels = GetChannelsToCheck();

            TwitchTimer = new Timer(1000 * 60 * UpdateIntervalMinutes);
            TwitchTimer.Elapsed += TwitchTimer_Elapsed;
            TwitchTimer.Enabled = true;
        }

        private void OnListTwitchChannels(string arg, string channel, string nick, string replyTo)
        {
            var setting = GetSetting(channel) as List<string>;
            if (setting == null || setting.Count() == 0)
            {
                irc.SendMessage(SendType.Message, replyTo, "No twitch channels subscribed in " + channel);
            }
            else
            {
                irc.SendMessage(SendType.Message, replyTo,
                    string.Format("[X] => Twitch channel online", UpdateIntervalMinutes));
                setting.ForEach(tc =>
                    irc.SendMessage(SendType.Message, replyTo,
                        string.Format("[{0}] {1}", OnlineChannels.Contains(tc) ? "X" : " ", tc)));
            }

        }

        private void OnAddTwitchChannel(string arg, string channel, string nick, string replyTo)
        {
            var setting = GetSetting(channel) as List<string> ?? new List<string>();
            if (!setting.Exists(s => s.Equals(arg, StringComparison.CurrentCultureIgnoreCase)))
            {
                string propername = GetTwitchChannelName(arg);
                if (!string.IsNullOrEmpty(propername))
                {
                    setting.Add(propername);
                    SetSetting(channel, setting);
                    if (!OnlineChannels.Contains(propername) && IsChannelOnline(propername))
                    {
                        OnlineChannels.Add(propername);
                    }
                }
                else
                {
                    irc.SendMessage(SendType.Message, replyTo, "Could not find Twitch channel " + arg);
                }
            }
        }

        private void OnRemoveTwitchChannel(string arg, string channel, string nick, string replyTo)
        {
            var setting = GetSetting(channel) as List<string> ?? new List<string>();
            if (setting.Remove(arg))
                SetSetting(channel, setting);
        }

        void TwitchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckChannelStatuses();
        }

        private string GetTwitchChannelName(string twitchChannel)
        {
            try
            {
                var r = PluginUtils.DownloadWebPage("https://api.twitch.tv/kraken/channels/" + twitchChannel);
                if (r.Length > 0)
                {
                    var tr = new JavaScriptSerializer().Deserialize<TwitchChannelsResponse>(r);
                    if (string.IsNullOrEmpty(tr.error) && !string.IsNullOrEmpty(tr.name))
                        return tr.name;
                }
            }
            catch { }
            return null;
        }

        private bool IsChannelOnline(string channel)
        {
            try
            {
                var r = PluginUtils.DownloadWebPage("https://api.twitch.tv/kraken/streams?channel=" + channel);
                if (r.Length > 0)
                {
                    var tr = new JavaScriptSerializer().Deserialize<TwitchStreamsResponse>(r);
                    if (tr.streams != null && tr.streams.Count() > 0)
                        return true;
                }
            }
            catch { }
            return false;
        }

        private List<string> GetChannelsToCheck()
        {
            List<string> ChannelsToCheck = new List<string>();

            foreach (var ch in EnabledChannels)
            {
                var s = GetSetting(ch) as List<string>;
                if (s != null)
                    ChannelsToCheck.AddRange(s);
            }

            return ChannelsToCheck.Distinct().ToList();
        }

        private void CheckChannelStatuses()
        {
            List<string> ChannelsToCheck = GetChannelsToCheck();

            if (ChannelsToCheck.Count() > 0)
            {
                string url = "https://api.twitch.tv/kraken/streams?channel=" + string.Join(",", ChannelsToCheck);
                try
                {
                    var r = PluginUtils.DownloadWebPage(url);
                    if (r.Length > 0)
                    {
                        var tr = new JavaScriptSerializer().Deserialize<TwitchStreamsResponse>(r);
                        if (tr.streams != null && tr.streams.Count() > 0)
                        {
                            List<string> streams = tr.streams.Select(s => s.channel.name).ToList();
                            foreach (var stream in streams.Where(s => !OnlineChannels.Exists(c => c == s)))
                            {
                                foreach (string ch in EnabledChannels)
                                {
                                    var channel = tr.streams.FirstOrDefault(s => s.channel.name == stream).channel;
                                    string status = string.IsNullOrEmpty(channel.status) ? "" : "/ '" + channel.status + "'";
                                    string chUrl = GetShortUrl(channel.url);

                                    var setting = GetSetting(ch) as List<string>;
                                    if (setting != null && setting.Contains(stream))
                                    {
                                        irc.SendMessage(SendType.Message, ch, string.Format("{3}{0}{3} is now live on Twitch {1} / {2}",
                                            stream, status.Trim(), chUrl, PluginUtils.IrcConstants.IrcBold));
                                    }
                                }
                            }

                            OnlineChannels = streams;
                        }

                        else if (OnlineChannels.Count() > 0)
                        {
                            OnlineChannels.Clear();
                        }
                    }
                }
                catch { }
            }
        }

        private string GetShortUrl(string url)
        {
            string shortUrl;
            if (Urls.TryGetValue(url, out shortUrl))
                return shortUrl;

            shortUrl = PluginUtils.CreateShortUrl(url);
            if (shortUrl.Length > 0)
            {
                Urls.Add(url, shortUrl);
                return shortUrl;
            }
            return url;
        }

        #region Twitch JSON Classes
        class TwitchStreamsResponse
        {
            public int _total { get; set; }
            public List<Stream> streams { get; set; }

            public class Stream
            {
                public string game { get; set; }
                public int viewers { get; set; }
                public Channel channel { get; set; }
            }

            public class Channel
            {
                public string status { get; set; }
                public string display_name { get; set; }
                public string name { get; set; }
                public string url { get; set; }
            }
        }

        class TwitchChannelsResponse
        {
            public string error;
            public string status;
            public string message;
            public string name;
        }
        #endregion
    }
}
