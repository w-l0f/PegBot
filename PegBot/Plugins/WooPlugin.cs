using System;
using System.Collections.Generic;
using Meebey.SmartIrc4net;
using System.Timers;

namespace PegBot.Plugins
{
    class WooPlugin : BotPlugin
    {
        public Dictionary<string, Timer> TimeOut;

        public WooPlugin(IrcClient irc)
            : base(irc, "Woo/Boo")
        {
            TimeOut = new Dictionary<string, Timer>();
            RegisterCommand(".woo", "<nick>", "woo on <nick>", OnWoo, false);
            RegisterCommand(".boo", "<nick>", "boo on <nick>", OnBoo, false);
            RegisterCommand(".wooboo", "List of woo and boo", OnWooBoo, false);
            RegisterCommand(".woo-remove", "<nick>", "Remove <nick>", OnRemove);
        }

        private void OnWoo(string arg, string channel, string nick, string replyTo)
        {
            setWooBoo(nick, arg, channel, replyTo, true);
        }

        private void OnBoo(string arg, string channel, string nick, string replyTo)
        {
            setWooBoo(nick, arg, channel, replyTo, false);
        }

        private void OnWooBoo(string arg, string channel, string nick, string replyTo)
        {
            int maxLength = 0;
            List<WooBooData> WooList = GetSetting(channel) as List<WooBooData> ?? new List<WooBooData>();
            WooList.Sort();
            irc.SendMessage(SendType.Message, replyTo, "Woo/Boo toplist in " + channel);
            WooList.ForEach(delegate (WooBooData Woo)
            {
                if (Woo.Nick.Length > maxLength)
                    maxLength = Woo.Nick.Length;
            });
            foreach (WooBooData WooData in WooList)
            {
                irc.SendMessage(SendType.Message, replyTo, string.Format(
                    "{0}: Score {1} (woo: {2}, boo: {3})",
                    WooData.Nick.PadRight(maxLength),
                    WooData.Score.ToString().PadRight(3),
                    WooData.Woo.ToString(),
                    WooData.Boo.ToString()));
            }
        }

        private void OnRemove(string arg, string channel, string nick, string replyTo)
        {
            List<WooBooData> WooList = GetSetting(channel) as List<WooBooData> ?? new List<WooBooData>();
            WooBooData WooData = WooList.Find(p => p.Nick.Equals(nick, StringComparison.CurrentCulture));
            if (WooData == null)
            {
                irc.SendMessage(SendType.Message, replyTo, "No such nick: " + nick);
            }
            else
            {
                WooList.Remove(WooData);
                SetSetting(channel, WooList);
            }
        }

        private void setWooBoo(string nick, string arg, string channel, string replyTo, bool woo)
        {
            if (irc.GetChannelUser(channel, arg) == null)
            {
                irc.SendMessage(SendType.Message, replyTo, arg + " not in channel");
                return;
            }
            if (TimeOut.ContainsKey(nick))
            {
                TimeOut[nick].Stop();
                TimeOut[nick].Start();
                irc.SendMessage(SendType.Message, replyTo, nick + ": Timeout 1m");
                return;
            }
            List<WooBooData> WooList = GetSetting(channel) as List<WooBooData> ?? new List<WooBooData>();
            WooBooData WooData = WooList.Find(p => p.Nick.Equals(arg, StringComparison.OrdinalIgnoreCase));
            if(WooData == null)
            {
                WooData = new WooBooData(arg);
                WooList.Add(WooData);
            }
            WooData.Increase(woo);
            irc.SendMessage(SendType.Message, replyTo, string.Format("{0}: Score {1} (woo: {2}, boo: {3})", 
                WooData.Nick, 
                WooData.Score, 
                WooData.Woo, 
                WooData.Boo));
            SetSetting(channel, WooList);

            Timer Time = new Timer(60 * 1000);
            Time.Elapsed += RemoveTimeOut;
            Time.Start();
            TimeOut.Add(nick, Time);
        }

        private void RemoveTimeOut(object sender, ElapsedEventArgs e)
        {
            foreach (KeyValuePair<string, Timer> entry in TimeOut)
            {
                if(entry.Value.Equals(sender))
                {
                    TimeOut.Remove(entry.Key);
                    break;
                }
            }
        }

        [Serializable]
        private class WooBooData : IComparable<WooBooData>
        {
            public string Nick { get; set; }
            public int Woo { get; set; }
            public int Boo { get; set; }
            public int Score { get { return Woo - Boo; } }

            public WooBooData(string Nick)
            {
                this.Nick = Nick;
                this.Woo = 0;
                this.Boo = 0;
            }

            public void Increase(bool Woo)
            {
                if (Woo)
                    this.Woo += 1;
                else
                    this.Boo += 1;
            }

            public int CompareTo(WooBooData other)
            {
                return other.Score - Score;
            }
        }
    }
}
