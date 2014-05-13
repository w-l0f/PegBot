using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace PegBot.Plugins
{
    class TwitchPlugin : BotPlugin
    {
        public TwitchPlugin(IrcClient irc)
            : base(irc, "Twitch")
        {
            irc.OnChannelMessage += new IrcEventHandler(OnChannelMessage);
        }

        private void OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (ChannelEnabled(e.Data.Channel))
            {
                if(e.Data.Message.StartsWith(".twitch ", StringComparison.CurrentCultureIgnoreCase))
                {
                    string[] split = e.Data.Message.Split(' ');
                    if (split.Count() == 2 && split[1].Equals("list", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var setting = GetSetting(e.Data.Channel) as List<string>;
                        if (setting == null)
                            irc.SendMessage(SendType.Message, e.Data.Channel, "No twitch channels subscribed in " + e.Data.Channel);
                        else
                            setting.ForEach(tc => irc.SendMessage(SendType.Message, e.Data.Channel, tc));
                    }
                    else if (split.Count() == 3)
                    {
                        if (split[1].Equals("add", StringComparison.CurrentCultureIgnoreCase))
                        {
                            AddTwitchChannel(e.Data.Channel, split[2]);
                            //add split[2]
                        }
                        else if (split[2].Equals("remove", StringComparison.CurrentCultureIgnoreCase))
                        {
                            //remove split[2]
                        }
                    }
                }
                
            }
        }

        private void AddTwitchChannel(string channel, string twitchChannel)
        {
            var setting = GetSetting(channel) as List<string> ?? new List<string>();
            if(!setting.Exists(s => s.Equals(twitchChannel, StringComparison.CurrentCultureIgnoreCase)))
            {
                string propername = GetTwitchChannelName(twitchChannel);
                if (!string.IsNullOrEmpty(propername))
                {
                    setting.Add(propername);
                    SetSetting(channel, setting);
                }
                    
                else
                {
                    irc.SendMessage(SendType.Message, channel, "Could not find Twitch channel " + twitchChannel); 
                }
            }
        }

        private string GetTwitchChannelName(string twitchChannel)
        {
            try
            {
                var r = new WebClient().DownloadString("https://api.twitch.tv/kraken/channels/" + twitchChannel);
                TwitchResponse tr = new JavaScriptSerializer().Deserialize<TwitchResponse>(r);
                if (string.IsNullOrEmpty(tr.error) && !string.IsNullOrEmpty(tr.name))
                    return tr.name;
            } catch { }
            return null;
        }

        public override string[] GetHelpCommands()
        {
            String[] commands = { ".twitch <add/remove> <twitchchannel> -- Add or remove specified twitch channel to/from subscription list",
                                  ".twitch list -- List all currently subscribed twitch channels" };
            return commands;
        }

        class TwitchResponse 
        {
            public string error;
            public string status;
            public string message;
            public string name;
        }

    }
}
