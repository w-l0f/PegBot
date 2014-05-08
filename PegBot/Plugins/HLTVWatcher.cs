using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;
using System.Collections;
using System.Timers;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.ServiceModel;
using System.ServiceModel.Syndication;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PegBot.Plugins
{
    class HLTVWatcher : BotPlugin
    {
        private List<String> IntrestingTeams;
        private List<Match> UpcomingMatches;

        private Timer minuteTimer;
        private Timer hltvTimer;
        private int updateRate = 15;
        private float updateRandomness = 0.2F;

        public HLTVWatcher(IrcClient irc)
            : base(irc, "HLTV-Watcher plugin")
        {
            IntrestingTeams = new List<string>();
            UpcomingMatches = new List<Match>();
            ReadIntrestingTeams();

            minuteTimer = new Timer(1000 * 60);
            minuteTimer.Elapsed += new ElapsedEventHandler(updateMinute);
            minuteTimer.Enabled = true;

            hltvTimer = new Timer(1000);
            hltvTimer.Elapsed += new ElapsedEventHandler(updateHLTV);
            hltvTimer.Enabled = true;

            irc.OnChannelMessage += new IrcEventHandler(OnChannelMessage);
        }

        private void OnChannelMessage(object sender, IrcEventArgs e)
        {
            string[] message = e.Data.Message.Split(new char[] { ' ' }, 2);

            if (message[0] == ".hltv-watch")
            {
                string team = message[1].Trim();
                if (String.IsNullOrEmpty(team))
                {
                    irc.SendMessage(SendType.Message, e.Data.Channel, "No team specified");
                    return;
                }
                if (AddWatchTeam(team))
                    irc.SendMessage(SendType.Message, e.Data.Channel, "Now watching " + team);
                else
                    irc.SendMessage(SendType.Message, e.Data.Channel, "Already watching " + team);
                return;
            }

            if (message[0] == ".hltv-unwatch")
            {
                string team = message[1].Trim();
                if (String.IsNullOrEmpty(team))
                {
                    irc.SendMessage(SendType.Message, e.Data.Channel, "No team specified");
                    return;
                }
                if (RemoveWatchTeam(team))
                    irc.SendMessage(SendType.Message, e.Data.Channel, "Unwatching " + team);
                else
                    irc.SendMessage(SendType.Message, e.Data.Channel, "No watch for " + team);
                return;
            }

            if (message[0] == ".hltv-watchlist")
            {
                if (IntrestingTeams.Count == 0)
                    irc.SendMessage(SendType.Message, e.Data.Channel, "Not watching any teams");
                else
                {
                    irc.SendMessage(SendType.Message, e.Data.Channel, "Watching teams: ");
                    foreach (string team in IntrestingTeams)
                        irc.SendMessage(SendType.Message, e.Data.Channel, team);
                }
            }

            if (message[0] == ".hltv-allmatches")
            {
                if (UpcomingMatches.Count == 0)
                    irc.SendMessage(SendType.Message, e.Data.Channel, "No upcoming matches");
                else
                {
                    irc.SendMessage(SendType.Message, e.Data.Channel, "Upcoming matches: ");
                    foreach (Match match in UpcomingMatches)
                        irc.SendMessage(SendType.Message, e.Data.Channel, match.PlayDate.TimeOfDay.ToString() + " " + match.ToString());
                }
            }

            if (message[0] == ".hltv-match" || message[0] == ".hltv-matches")
            {
                List<Match> matches = new List<Match>();
                foreach (Match match in UpcomingMatches)
                    if (isIntresting(match))
                        matches.Add(match);

                if (matches.Count == 0)
                    irc.SendMessage(SendType.Message, e.Data.Channel, "No upcoming matches");
                else
                {
                    irc.SendMessage(SendType.Message, e.Data.Channel, "Upcoming matches: ");
                    foreach (Match match in matches)
                        irc.SendMessage(SendType.Message, e.Data.Channel, match.PlayDate.TimeOfDay.ToString() + " " + match.ToString());
                }
            }

            if (message[0] == ".hltv-update")
            {
                irc.SendMessage(SendType.Message, e.Data.Channel, "Updating...");
                updateHLTV(null, null);
            }
        }

        private bool AddWatchTeam(string team)
        {
            if (IntrestingTeams.Contains(team))
                return false;
            IntrestingTeams.Add(team);
            SaveIntrestingTeams();
            return true;
        }

        private bool RemoveWatchTeam(string team)
        {
            if (!IntrestingTeams.Contains(team))
                return false;
            IntrestingTeams.Remove(team);
            SaveIntrestingTeams();
            return true;
        }

        private void SaveIntrestingTeams()
        {
            try
            {
                using (Stream stream = File.Open("IntrestingChannels.txt", FileMode.Create))
                {
                    new BinaryFormatter().Serialize(stream, IntrestingTeams);
                }
            }
            catch (IOException)
            {
            }
        }

        private void ReadIntrestingTeams()
        {
            try
            {
                using (Stream stream = File.Open("IntrestingChannels.txt", FileMode.Open))
                {
                    IntrestingTeams = (List<string>)new BinaryFormatter().Deserialize(stream);
                }
            }
            catch (IOException)
            {
            }
        }

        public override string[] GetHelpCommands()
        {
            String[] commands = {".hltv-watch <team> -- Hilight when <team> have match",
                                ".hltv-unwatch <team> -- Remove hilight on <team>",
                                ".hltv-watchlist -- Print list of teams watched",
                                ".hltv-matches -- Print all upcoming matches",
                                ".hltv-allmatches -- Print all upcoming matches",
                                ".hltv-update -- Updates upcoming matches"};
            return commands;
        }

        private bool isIntresting(Match match)
        {
            foreach (string intresting in IntrestingTeams)
            {
                if (intresting.ToLower() == match.Team1.ToLower() || intresting.ToLower() == match.Team2.ToLower())
                    return true;
            }
            return false;
        }

        private void updateMinute(object source, ElapsedEventArgs e)
        {
            List<Match> removeMatches = new List<Match>();
            foreach (Match match in UpcomingMatches)
            {
                if (!match.hasBroadcasted && isIntresting(match))
                {
                    if (match.PlayDate.CompareTo(DateTimeOffset.Now) <= 0)
                    {
                        //todo: 
                        //foreach(string channel in enabledchannels) 
                        //irc.SendMessage(SendType.Message, channel, "Now starting " + match.ToString());
                        //---
                        irc.SendMessage(SendType.Message, "#pegbot", "Now starting " + match.ToString());
                        match.hasBroadcasted = true;
                    }
                }
                if (match.PlayDate.CompareTo(DateTimeOffset.Now.AddDays(-1)) <= 0)
                    removeMatches.Add(match);
            }
            foreach (Match remove in removeMatches)
                UpcomingMatches.Remove(remove);
        }

        private void updateHLTV(object source, ElapsedEventArgs e)
        {
            int randomness = new Random().Next(-(int)(updateRate * updateRandomness * 60 * 1000), (int)(updateRate * updateRandomness * 60 * 1000));
            hltvTimer.Interval = updateRate * 60 * 1000 + randomness;

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
                Match oldmatch = UpcomingMatches.Find(p => p.CompareTo(newMatch) == 0);
                if (oldmatch == null)
                    UpcomingMatches.Add(newMatch);
                else
                    if (oldmatch.PlayDate.CompareTo(newMatch.PlayDate) != 0)
                        oldmatch.PlayDate = newMatch.PlayDate;
            }
        }

        class Match : IComparable
        {
            public readonly string Team1;
            public readonly string Team2;
            public readonly string MatchPage;
            public DateTimeOffset PlayDate;
            public bool hasBroadcasted;

            public Match(string Team1, string Team2, string MatchPage)
            {
                this.Team1 = Team1;
                this.Team2 = Team2;
                this.MatchPage = MatchPage;
                hasBroadcasted = false;
            }

            public int CompareTo(object obj)
            {
                if (obj == null)
                    return 1;
                Match otherMatch = obj as Match;
                if (otherMatch != null)
                {
                    if (MatchPage == String.Empty && otherMatch.MatchPage == String.Empty)
                    {
                        if ((Team1 + Team2).CompareTo(otherMatch.Team1 + otherMatch.Team2) == 0)
                            return 0;
                    }
                    else
                    {
                        if (MatchPage.CompareTo(otherMatch.MatchPage) == 0)
                            return 0;
                    }
                    int datecmp = PlayDate.CompareTo(otherMatch.PlayDate);
                    if (datecmp != 0)
                        return datecmp;
                    else
                        return -1;
                }
                else
                    throw new ArgumentException("Object is not a Match-object");
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Team1);
                sb.Append(" vs ");
                sb.Append(Team2);
                sb.Append(" (");
                sb.Append(MatchPage);
                sb.Append(")");
                return sb.ToString();
            }
        }
    }
}
