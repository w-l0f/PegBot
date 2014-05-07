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

namespace PegBot.Plugins
{
    class HLTVWatcher : BotPlugin
    {
        private List<String> IntrestingTeams;
        private List<Match> UpcomingMatches;

        private Timer hltvTimer;
        private int updateRate = 15;
        private float updateRandomness = 0.2F;

        public HLTVWatcher(IrcClient irc)
            : base(irc, "HLTV-Watcher plugin")
        {
            IntrestingTeams = new List<string>(); //todo: load data from file
            UpcomingMatches = new List<Match>();

            hltvTimer = new Timer(1000);
            hltvTimer.Elapsed += new ElapsedEventHandler(update);
            hltvTimer.Enabled = true;
            irc.OnChannelMessage += new IrcEventHandler(OnChannelMessage);
        }

        private void OnChannelMessage(object sender, IrcEventArgs e)
        {
            if(e.Data.Message.StartsWith(".hltv-watch "))
            {

            }
            if(e.Data.Message.StartsWith(".hltv-unwatch "))
            {

            }
            if(e.Data.Message.StartsWith(".hltv-watchlist "))
            {

            }
            if(e.Data.Message.StartsWith(".hltv-matches "))
            {

            }
            if(e.Data.Message.StartsWith(".hltv-update "))
            {

            }
        }



        public override string[] GetHelpCommands()
        {
            String[] commands = {".hltv-watch <team> -- Hilight when <team> have match",
                                ".hltv-unwatch <team> -- Remove hilight on <team>",
                                ".hltv-watchlist -- Print list of teams watched",
                                ".hltv-matches -- Print all upcoming matches", 
                                ".hltv-update -- Updates upcoming matches"};
            return commands;
        }

        private void update(object source, ElapsedEventArgs e)
        {
            int randomness = new Random().Next(-(int)(updateRate * updateRandomness * 60 * 1000), (int)(updateRate * updateRandomness * 60 * 1000));
            hltvTimer.Interval = updateRate * 60 * 1000 + randomness;
        
            List<Match> newMatches = new List<Match>();
            SyndicationFeed feed = SyndicationFeed.Load(XmlReader.Create("http://www.hltv.org/hltv.rss.php?pri=15"));
            
            foreach(SyndicationItem item in feed.Items)
            {
                int vs = item.Title.Text.IndexOf(" vs ");
                if (vs < 1)
                    return;
                string Team1 = item.Title.Text.Substring(0, vs);
                string Team2 = item.Title.Text.Substring(vs + 4);
                string MatchPage = item.Links[0].Uri.AbsoluteUri;
                DateTimeOffset PlayDate = item.PublishDate;
                Match newMatch = new Match(Team1, Team2, MatchPage);
                newMatch.PlayDate = PlayDate;
                newMatches.Add(newMatch);
                Console.WriteLine(newMatch.ToString());
            }

            foreach(Match newMatch in newMatches)
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
            bool hasBroadCasted;

            public Match(string Team1, string Team2, string MatchPage)
            {
                this.Team1 = Team1;
                this.Team2 = Team2;
                this.MatchPage = MatchPage;
                hasBroadCasted = true;
            }

            public int CompareTo(object obj)
            {
                if (obj == null)
                    return 1;
                Match otherMatch = obj as Match;
                if (otherMatch != null)
                {
                    if (MatchPage == String.Empty && otherMatch.MatchPage == String.Empty)
                        return (Team1 + Team2).CompareTo(otherMatch.Team1 + otherMatch.Team2);
                    else
                        return MatchPage.CompareTo(otherMatch.MatchPage);
                }
                else
                    throw new ArgumentException("Object is not a Match-object");
            }

            public string ToString()
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
