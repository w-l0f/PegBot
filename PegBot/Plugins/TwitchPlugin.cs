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
            RegisterCommand(".twitch clientid", "<Client-ID>", "use <Client-ID> as twitch client id (api key) for this channel", OnClientId);
            //add all channels as online in order no to spam
            OnlineChannels = GetChannelsToCheck();

            TwitchTimer = new Timer(1000 * 60 * UpdateIntervalMinutes);
            TwitchTimer.Elapsed += TwitchTimer_Elapsed;
            TwitchTimer.Enabled = true;
        }

        private TwitchChannelSetting GetTwitchSetting(string channel)
        {
            object setting = GetSetting(channel);
            if (setting is TwitchChannelSetting)
            {
                return setting as TwitchChannelSetting;
            }

            //backwards compatible to previous version
            if (setting is List<string>)
            {
                return new TwitchChannelSetting() { ClientId = "", Channels = setting as List<string> };
            }

            return new TwitchChannelSetting();
        }

        private void OnListTwitchChannels(string arg, string channel, string nick, string replyTo)
        {
            var setting = GetTwitchSetting(channel);
            if (setting == null || setting.Channels == null || setting.Channels.Count() == 0)
            {
                irc.SendMessage(SendType.Message, replyTo, "No twitch channels subscribed in " + channel);
            }
            else
            {
                irc.SendMessage(SendType.Message, replyTo,
                    string.Format("[X] => Twitch channel online", UpdateIntervalMinutes));
                setting.Channels.ForEach(tc =>
                    irc.SendMessage(SendType.Message, replyTo,
                        string.Format("[{0}] {1}", OnlineChannels.Contains(tc) ? "X" : " ", tc)));
            }

        }

        private void OnAddTwitchChannel(string arg, string channel, string nick, string replyTo)
        {
            var setting = GetTwitchSetting(channel);
            if (!setting.Channels.Exists(s => s.Equals(arg, StringComparison.CurrentCultureIgnoreCase)))
            {
                string propername = GetTwitchChannelName(arg, setting.ClientId);
                if (!string.IsNullOrEmpty(propername))
                {
                    setting.Channels.Add(propername);
                    SetSetting(channel, setting);
                    if (!OnlineChannels.Contains(propername) && IsChannelOnline(propername, setting.ClientId))
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
            var setting = GetTwitchSetting(channel);
            if (setting.Channels.Remove(arg))
                SetSetting(channel, setting);
        }

        private void OnClientId(string arg, string channel, string nick, string replyTo)
        {
            var setting = GetTwitchSetting(channel);
            setting.ClientId = arg;
            SetSetting(channel, setting);
        }

        void TwitchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckChannelStatuses();
        }

        private string GetTwitchChannelName(string twitchChannel, string clientId)
        {
            try
            {
                var h = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(clientId))
                    h.Add("Client-ID", clientId);

                var r = PluginUtils.DownloadWebPage("https://api.twitch.tv/kraken/channels/" + twitchChannel, false, h);
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

        private bool IsChannelOnline(string channel, string clientId)
        {
            try
            {
                var h = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(clientId))
                    h.Add("Client-ID", clientId);

                var r = PluginUtils.DownloadWebPage("https://api.twitch.tv/kraken/streams?channel=" + channel, false, h);
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
                var s = GetTwitchSetting(ch);
                if (s != null)
                    ChannelsToCheck.AddRange(s.Channels);
            }

            return ChannelsToCheck.Distinct().ToList();
        }

        private void CheckChannelStatuses()
        {
            var onlineChannels = new List<string>();
            foreach (var ch in EnabledChannels)
            {
                var setting = GetTwitchSetting(ch);

                if (setting.Channels.Count() > 0)
                {
                    string url = "https://api.twitch.tv/kraken/streams?channel=" + string.Join(",", setting.Channels);
                    try
                    {
                        var h = new Dictionary<string, string>();
                        if (!string.IsNullOrEmpty(setting.ClientId))
                            h.Add("Client-ID", setting.ClientId);

                        var r = PluginUtils.DownloadWebPage(url, false, h);
                        if (r.Length > 0)
                        {
                            var tr = new JavaScriptSerializer().Deserialize<TwitchStreamsResponse>(r);
                            if (tr.streams != null && tr.streams.Count() > 0)
                            {
                                foreach (var stream in tr.streams.Where(s => !OnlineChannels.Exists(c => c == s.channel.name)))
                                {
                                    var channel = stream.channel;
                                    string status = string.IsNullOrEmpty(channel.status) ? "" : "/ '" + channel.status + "'";
                                    string chUrl = GetShortUrl(channel.url);

                                    irc.SendMessage(SendType.Message, ch, string.Format("{3}{0}{3} is now live on Twitch / playing {4} {1} / {2}",
                                        channel.name, status.Trim(), chUrl, PluginUtils.IrcConstants.IrcBold, stream.game));
                                }

                                onlineChannels.AddRange(tr.streams.Select(s => s.channel.name));
                            }
                        }
                    }
                    catch { }
                }
            }

            OnlineChannels = onlineChannels.Distinct().ToList();
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

        [Serializable]
        protected class TwitchChannelSetting
        {
            public string ClientId;
            public List<string> Channels = new List<string>();
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