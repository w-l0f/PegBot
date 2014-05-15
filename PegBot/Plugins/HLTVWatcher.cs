using System;
using System.Collections.Generic;
using System.Text;
using Meebey.SmartIrc4net;
using System.Timers;
using System.Xml;
using System.ServiceModel.Syndication;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Net;
using System.Collections.Specialized;
using System.Web.Script.Serialization;

namespace PegBot.Plugins
{
    class HLTVWatcher : BotPlugin
    {
        private List<Match> UpcomingMatches;

        private Timer minuteTimer;
        private Timer hltvTimer;
        private int updateRate = 15;
        private float updateRandomness = 0.2F;

        public HLTVWatcher(IrcClient irc)
            : base(irc, "HLTV-Watcher")
        {
            UpcomingMatches = new List<Match>();

            minuteTimer = new Timer(1000 * 60);
            minuteTimer.Elapsed += new ElapsedEventHandler(updateMinute);
            minuteTimer.Enabled = true;

            hltvTimer = new Timer(1000);
            hltvTimer.Elapsed += new ElapsedEventHandler(updateHLTV);
            hltvTimer.Enabled = true;

            Subscribe(".hltv watch", "<team>", "Hilight when <team> have match", OnWatch);
            Subscribe(".hltv unwatch", "<team>", "Remove hilight on <team>", OnUnWatch);
            Subscribe(".hltv list", "Print list of teams watched", OnWatchlist, false);
            Subscribe(".hltv match", "[all]", "Print upcoming matches", OnMatch, arg => String.IsNullOrEmpty(arg) || arg == "all", false);
            Subscribe(".hltv update", "Updates upcoming matches", OnHltvUpdate);
        }

        private void OnWatch(string arg, string channel, string nick, string replyTo)
        {
            List<string> SubscribedTeams = GetSetting(channel) as List<string> ?? new List<string>();
            if (SubscribedTeams.Contains(arg))
                irc.SendMessage(SendType.Message, replyTo, "Already watching " + arg);
            else
            {
                SubscribedTeams.Add(arg);
                SetSetting(channel, SubscribedTeams);
            }
        }

        private void OnUnWatch(string arg, string channel, string nick, string replyTo)
        {
            List<string> SubscribedTeams = GetSetting(channel) as List<string>;
            if (SubscribedTeams == null || !SubscribedTeams.Remove(arg))
                irc.SendMessage(SendType.Message, replyTo, "No watch for " + arg);
            else
                SetSetting(channel, SubscribedTeams);
        }

        private void OnWatchlist(string arg, string channel, string nick, string replyTo)
        {
            List<string> SubscribedTeams = GetSetting(channel) as List<string>;
            if (SubscribedTeams == null || SubscribedTeams.Count == 0)
                irc.SendMessage(SendType.Message, replyTo, "Not watching any teams");
            else
            {
                foreach (string team in SubscribedTeams)
                    irc.SendMessage(SendType.Message, replyTo, team);
            }
        }

        private void OnMatch(string arg, string channel, string nick, string replyTo)
        {
            List<Match> matches = UpcomingMatches;
            if (arg != "all")
            {
                List<string> SubscribedTeams = GetSetting(channel) as List<string> ?? new List<string>();
                matches = UpcomingMatches.FindAll(match => SubscribedTeams.Contains(match.Team1, StringComparer.OrdinalIgnoreCase) || SubscribedTeams.Contains(match.Team2, StringComparer.OrdinalIgnoreCase));
            }

            if (matches.Count == 0)
                irc.SendMessage(SendType.Message, replyTo, "No upcoming matches");
            else
            {
                foreach (Match match in matches)
                    irc.SendMessage(SendType.Message, replyTo, match.PlayDate.TimeOfDay.ToString() + " " + match.ToString());
            }
        }

        private void OnHltvUpdate(string arg, string channel, string nick, string replyTo)
        {
            updateHLTV(null, null);
        }

        private void updateMinute(object source, ElapsedEventArgs e)
        {
            foreach (string channel in EnabledChannels)
            {
                List<string> SubscribedTeams = GetSetting(channel) as List<string> ?? new List<string>();
                foreach (Match match in UpcomingMatches.FindAll(match => SubscribedTeams.Contains(match.Team1, StringComparer.OrdinalIgnoreCase) || SubscribedTeams.Contains(match.Team2, StringComparer.OrdinalIgnoreCase)))
                {
                    if (match.Broadcast(channel) && match.PlayDate.CompareTo(DateTimeOffset.Now) <= 0 && match.PlayDate.CompareTo(DateTimeOffset.Now.AddHours(-1)) <= 0)
                        irc.SendMessage(SendType.Message, channel, "Now starting " + match.ToString());
                }
            }
            UpcomingMatches.RemoveAll(m => m.PlayDate.CompareTo(DateTimeOffset.Now.AddDays(-1)) <= 0);
        }

        private void updateHLTV(object source, ElapsedEventArgs e)
        {
            int randomness = new Random().Next(-(int)(updateRate * updateRandomness * 60 * 1000), (int)(updateRate * updateRandomness * 60 * 1000));
            hltvTimer.Interval = updateRate * 60 * 1000 + randomness;
            try
            {
                List<Match> newMatches = new List<Match>();
                SyndicationFeed feed = SyndicationFeed.Load(XmlReader.Create("http://www.hltv.org/hltv.rss.php?pri=15"));

                foreach (SyndicationItem item in feed.Items)
                {
                    int vs = item.Title.Text.IndexOf(" vs ");
                    if (vs < 1)
                        return;
                    string Team1 = item.Title.Text.Substring(0, vs);
                    string Team2 = item.Title.Text.Substring(vs + 4);
                    string MatchPage = String.Empty;
                    if (item.Links.Count > 0)
                        MatchPage = item.Links[0].Uri.AbsoluteUri;
                    DateTimeOffset PlayDate = item.PublishDate;
                    Match newMatch = new Match(Team1, Team2, MatchPage);
                    newMatch.PlayDate = PlayDate;
                    newMatches.Add(newMatch);
                }

                foreach (Match newMatch in newMatches)
                {
                    Match oldmatch = UpcomingMatches.Find(p => p.Equals(newMatch));
                    if (oldmatch == null)
                        UpcomingMatches.Add(newMatch);
                    else
                        if (oldmatch.PlayDate.CompareTo(newMatch.PlayDate) != 0)
                            oldmatch.PlayDate = newMatch.PlayDate;
                }
                UpcomingMatches.Sort();
            }
            catch (Exception) { };
        }

        class Match : IComparable
        {
            public readonly string Team1;
            public readonly string Team2;
            public readonly string MatchPage;
            public DateTimeOffset PlayDate;
            public string ShortUrl;
            private List<string> broadcasted;

            public Match(string Team1, string Team2, string MatchPage)
            {
                this.Team1 = Team1;
                this.Team2 = Team2;
                this.MatchPage = MatchPage;
                broadcasted = new List<string>();
            }

            public bool Broadcast(string channel)
            {
                if (broadcasted.Contains(channel))
                    return false;
                broadcasted.Add(channel);
                return true;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                Match otherMatch = obj as Match;

                if (MatchPage == String.Empty && otherMatch.MatchPage == String.Empty)
                {
                    if ((Team1 + Team2).Equals(otherMatch.Team1 + otherMatch.Team2))
                        return true;
                }
                else
                {
                    if (MatchPage.Equals(otherMatch.MatchPage))
                        return true;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return MatchPage.GetHashCode();
            }

            public int CompareTo(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return -1;
                Match otherMatch = obj as Match;
                return PlayDate.CompareTo(otherMatch.PlayDate);
            }

            public override string ToString()
            {
                if (String.IsNullOrEmpty(ShortUrl))
                    ShortUrl = PluginUtils.CreateShortUrl(MatchPage);

                StringBuilder sb = new StringBuilder();
                sb.Append(Team1);
                sb.Append(" vs ");
                sb.Append(Team2);
                sb.Append(" (");
                if (String.IsNullOrEmpty(ShortUrl))
                    sb.Append(MatchPage);
                else
                    sb.Append(ShortUrl);
                sb.Append(")");
                return sb.ToString();
            }
        }
    }
}
