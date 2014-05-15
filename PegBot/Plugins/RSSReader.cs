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
        private int updateTime = 10;
        private Timer RSSTimer;

        public RSSReader(IrcClient irc)
            : base(irc, "RSSReader")
        {
            RSSTimer = new Timer(1000 * 60 * updateTime);
            RSSTimer.Elapsed += RSSTimer_Elapsed;
            RSSTimer.Enabled = true;

            RegisterCommand(".rss add", "<short-name> <rss-url>", "Add rss watcher on <rss-url>", OnAdd, arg => arg.Split(' ').Length >= 2);
            RegisterCommand(".rss remove", "<short-name>", "Remove rss watcher", OnRemove);
            RegisterCommand(".rss list", "Print list of rss feeds active in this channel", OnList, false);
        }

        private void OnAdd(string arg, string channel, string nick, string replyTo)
        {
            Dictionary<string, string[]> Feeds = GetSetting(channel) as Dictionary<string, string[]> ?? new Dictionary<string, string[]>();
            string url = arg.Split(' ').Last();
            string id = arg.Substring(0, arg.Length - url.Length);

            if (Feeds.Keys.Contains(id))
                Feeds.Remove(id);
            string[] rssdata = new string[] {url, String.Empty };
            Feeds.Add(id, rssdata);
            SetSetting(channel, Feeds);
        }

        private void OnRemove(string arg, string channel, string nick, string replyTo)
        {
            Dictionary<string, string[]> Feeds = GetSetting(channel) as Dictionary<string, string[]>;
            if (Feeds != null && Feeds.Remove(arg))
                SetSetting(channel, Feeds);
        }

        private void OnList(string arg, string channel, string nick, string replyTo)
        {
            Dictionary<string, string[]> Feeds = GetSetting(channel) as Dictionary<string, string[]>;
            if (Feeds == null || Feeds.Count == 0)
                irc.SendMessage(SendType.Message, replyTo, "No rss-feeds in this channel");
            else
                foreach (string text in Feeds.Keys)
                    irc.SendMessage(SendType.Message, replyTo, "rss-feed: " + text);
        }

        void RSSTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (string channel in EnabledChannels)
            {
                Dictionary<string, string[]> Feeds = GetSetting(channel) as Dictionary<string, string[]> ?? new Dictionary<string, string[]>();
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
                                irc.SendMessage(SendType.Message, channel, item.Title.Text + " Author: " + item.Authors.First().Name);
                        }
                        Feeds[key][1] = feed.Items.First().Title.Text;
                    }
                    catch (Exception) { };
                }
            }
        }
    }
}
