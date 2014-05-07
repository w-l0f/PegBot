using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;
using System.Collections;

namespace PegBot.Plugins
{
    class PingPlugin : BotPlugin
    {
        public PingPlugin(IrcClient irc)
            : base(irc, "PingPlugin")
        {
            irc.OnChannelMessage += new IrcEventHandler(OnChannelMessage);
        }

        private void OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (e.Data.Message.ToLower().StartsWith(".ping"))
            {
                StringBuilder sb = new StringBuilder();
                Channel c = irc.GetChannel(e.Data.Channel);
                foreach (DictionaryEntry user in c.Users)
                {
                    //skip self
                    if (((string)user.Key) == irc.Nickname)
                        continue;
                    sb.Append(" ");
                    sb.Append(user.Key);
                }

                irc.SendMessage(SendType.Message, e.Data.Channel, "PING:" + sb.ToString());
                if (e.Data.Message.Length >= 6)
                    irc.SendMessage(SendType.Message, e.Data.Channel, ">>" + e.Data.Message.Substring(6));
            }
        }

        public override string[] GetHelpCommands()
        {
            String[] commands = { ".ping <message> -- Hilights everyone in channel with specified message" };
            return commands;
        }
    }
}
