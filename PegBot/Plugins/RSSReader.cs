using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace PegBot.Plugins
{
    class RSSReader : BotPlugin
    {
        private Dictionary<string, string[]> Feeds;
        private const string FILENAME_FEEDS = "feeds";
        private int updateTime = 10;
        private Timer RSSTimer;

        public RSSReader(IrcClient irc)
            : base(irc, "RSSReader")
        {
            RSSTimer = new Timer(1000 * updateTime);
            RSSTimer.Elapsed += RSSTimer_Elapsed;
            RSSTimer.Enabled = true;
            Feeds = new Dictionary<string, string[]>();
            try
            {
                Feeds = PluginUtils.LoadObject(FILENAME_FEEDS) as Dictionary<string, string[]>;
            }
            catch (IOException) { }

            irc.OnChannelMessage += OnChannelMessage;
            irc.OnQueryMessage += OnQueryMessage;
        }

        public override string[] GetHelpCommands()
        {
            String[] commands = {".rss-add <short-name> <rss-url> -- Hilight when <team> have match",
                                ".rss-remove <short-name> -- Remove hilight on <team>",
                                ".rss-list -- Print list of teams watched", 
                                };
            return commands;
        }

        void RSSTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (string key in Feeds.Keys)
            {
                try
                {
                    SyndicationFeed feed = SyndicationFeed.Load(XmlReader.Create(Feeds[key][0]));
                    foreach (SyndicationItem item in feed.Items)
                    {
                        if (String.IsNullOrEmpty(Feeds[key][1]))
                            Feeds[key][1] = item.Title.Text;
                        if (Feeds[key][1] == item.Title.Text)
                            break;
                        else
                        {
                            foreach(string channel in EnabledChannels)
                            {
                                irc.SendMessage(SendType.Message, channel, item.Title.Text + " Author: " + item.Authors.First().Name);
                            }
                        }
                    }
                    Feeds[key][1] = feed.Items.First().Title.Text;
                }
                catch (Exception) { };
            }
        }

        void OnQueryMessage(object sender, IrcEventArgs e)
        {
            string channel = e.Data.Message.Split(' ').Last();
            string message = e.Data.Message.Substring(0, e.Data.Channel.Length - channel.Length).Trim();
            if (ChannelEnabled(channel))
                ParseMessage(message, channel, e.Data.Nick, e.Data.Nick);
        }

        void OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (ChannelEnabled(e.Data.Channel))
                ParseMessage(e.Data.Message, e.Data.Channel, e.Data.Nick, e.Data.Channel);
        }

        private void ParseMessage(string message, string channel, string nick, string replyDestination)
        {
            if (message.Equals(".rss-list", StringComparison.CurrentCultureIgnoreCase))
            {
                if (Feeds.Count == 0)
                    irc.SendMessage(SendType.Message, replyDestination, "No rss-feeds");
                else
                    foreach (string text in Feeds.Keys)
                        irc.SendMessage(SendType.Message, replyDestination, "rss-feed: " + text);
            }

            string[] data = message.Split(' ');
            if (data[0].Equals(".rss-add", StringComparison.CurrentCultureIgnoreCase))
            {
                if (data.Length != 3)
                    irc.SendMessage(SendType.Message, replyDestination, "Wrong syntax, .rss-add <short-name> <rss-url>");
                else
                {
                    if (Feeds.Keys.Contains(data[1]))
                        Feeds.Remove(data[1]);
                    string[] rssdata = new string[] { data[2], String.Empty };
                    Feeds.Add(data[1], rssdata);
                    PluginUtils.SaveObject(Feeds, FILENAME_FEEDS);
                }
            }
            if (data[0].Equals("rss-remove", StringComparison.CurrentCultureIgnoreCase))
            {
                if (data.Length != 2)
                    irc.SendMessage(SendType.Message, replyDestination, "Wrong syntax, .rss-remove <short-name>");
                if (Feeds.Keys.Contains(data[1]))
                    Feeds.Remove(data[1]);
                PluginUtils.SaveObject(Feeds, FILENAME_FEEDS);
            }
        }
    }
}
