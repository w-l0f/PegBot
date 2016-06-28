using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;
using System.Timers;
using System.Web;
using System.Text.RegularExpressions;

namespace PegBot.Plugins
{
    class GamesDoneQuickPlugin : BotPlugin
    {
        private Timer UpdateTimer, AnnounceTimer;
        private const int UpdateIntervalMinutes = 60;
        private const int AnnounceIntervalMinutes = 1;
        private const string GDQUrl = "https://gamesdonequick.com/tracker/runs/";

        private List<GDQEvent> Events = new List<GDQEvent>();

        public GamesDoneQuickPlugin(IrcClient irc) : base(irc, "GamesDoneQuick")
        {
            RegisterCommand(".gdq next", "Shows next Games Done Quick-event", OnNext, false);

            UpdateTimer = new Timer(1000 * 60 * UpdateIntervalMinutes);
            UpdateTimer.Elapsed += UpdateTimer_Elapsed;
            UpdateTimer.Enabled = true;

            UpdateEvents();

            AnnounceTimer = new Timer(1000 * 60 * AnnounceIntervalMinutes);
            AnnounceTimer.Elapsed += AnnounceTimer_Elapsed;
            AnnounceTimer.Enabled = true;
        }

        private void AnnounceTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(Events.Count > 0)
            {
                var nextEvent = Events.OrderBy(xx => xx.StartTime).First();
                if (nextEvent.StartTime < DateTime.Now)
                {
                    Events.Remove(nextEvent);
                    var eventText = "Now on GDQ: " + GetEventText(nextEvent);

                    foreach (string ch in EnabledChannels)
                    {
                            irc.SendMessage(SendType.Message, ch, eventText);
                    }
                }
            }
        }

        private string GetEventText(GDQEvent nextEvent)
        {
            return string.Format("{3}{0}{3}{1} w/ {2}", 
                nextEvent.Title, 
                nextEvent.Description.Length > 0 ? " " + nextEvent.Description : "",
                nextEvent.Player,
                PluginUtils.IrcConstants.IrcBold
                );
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateEvents();
        }

        private void UpdateEvents()
        {
            try
            {
                var content = PluginUtils.DownloadWebPage(GDQUrl, false).Trim();
                content = HttpUtility.HtmlDecode(content);

                var events = new List<GDQEvent>();

                int parseIndex = 0;

                while (true)
                {
                    int startIndex = content.IndexOf("<tr class=\"small\">", parseIndex) + 18;
                    int stopIndex = content.IndexOf("</tr>", startIndex);

                    if (startIndex == -1 || stopIndex == -1)
                        break;

                    var gdqEvent = new GDQEvent();
                    var info = content.Substring(startIndex, stopIndex - startIndex);

                    int subIndex = 0;
                    subIndex = tryGetNextValue(info, subIndex, out gdqEvent.Title);
                    if (subIndex < 0) break;
                    subIndex = tryGetNextValue(info, subIndex, out gdqEvent.Player);
                    if (subIndex < 0) break;
                    subIndex = tryGetNextValue(info, subIndex, out gdqEvent.Description);
                    if (subIndex < 0) break;
                    subIndex = tryGetNextValue(info, subIndex, out gdqEvent.StartTime);
                    if (subIndex < 0) break;
                    subIndex = tryGetNextValue(info, subIndex, out gdqEvent.EndTime);
                    if (subIndex < 0) break;

                    if (gdqEvent.StartTime > DateTime.Now)
                        events.Add(gdqEvent);

                    parseIndex = stopIndex;
                }

                this.Events = events;
            }
            catch
            {
                PluginUtils.Log("GamesDoneQuickPlugin: Failed to parse GDQ Events!");
            }
            
        }
        
        private int tryGetNextValue(string eventInfo, int startIndex, out string value)
        {
            int index1 = eventInfo.IndexOf("<td>", startIndex);
            int index2 = eventInfo.IndexOf("</td>", startIndex);

            if(index1 > -1 && index2 > -1 && index2 > index1)
            {
                var raw = eventInfo.Substring(index1, index2 - index1);
                value = Regex.Replace(raw, "<.*?>", string.Empty).Trim();
                return index2 + 5;
            }

            value = null;
            return -1;
        }

        private int tryGetNextValue(string eventInfo, int startIndex, out DateTime value)
        {
            string strVal;
            int newIndex = tryGetNextValue(eventInfo, startIndex, out strVal);
            if(newIndex > -1)
            {
                value = DateTime.Parse(strVal);
            }
            else
            {
                value = DateTime.MinValue;
            }

            return newIndex;
        }

        private void OnNext(string arg, string channel, string nick, string replyTo)
        {
            var message = "No upcoming GDQ-run at this moment";
            if(Events.Count > 0)
            {
                var nextUp = Events.OrderBy(xx => xx.StartTime).First();
                message = string.Format("Next up on GDQ: {0} @ {1} {2}",
                    GetEventText(nextUp), nextUp.StartTime.ToShortDateString(), nextUp.StartTime.ToShortTimeString());
            }

            irc.SendMessage(SendType.Message, replyTo, message);
        }

        protected class GDQEvent
        {
            public DateTime StartTime;
            public DateTime EndTime;
            public string Title;
            public string Player;
            public string Description;
        }
    }
}
