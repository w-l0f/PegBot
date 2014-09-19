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
        private const int maxLength = 430; //about right, to be safe

        public PingPlugin(IrcClient irc)
            : base(irc, "Ping")
        {
            RegisterCommand(".ping", "[msg]", "Hilights everyone in channel with specified message", OnPing, arg => true, false);
        }

        private void OnPing(string arg, string channel, string nick, string replyTo)
        {
            if (arg.Length > 200)
            {
                irc.SendMessage(SendType.Message, channel, "ping is too long");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (DictionaryEntry user in irc.GetChannel(channel).Users)
                {
                    string un = (string)user.Key;
                    
                    //skip self
                    if (un == irc.Nickname)
                        continue;

                    if (nick.Length + sb.Length + arg.Length + 15 >= maxLength)
                    {
                        SendPing(channel, sb.ToString(), arg);
                        sb = new StringBuilder();
                        continue;
                    }

                    sb.Append(" ");
                    sb.Append(un);
                }
                if (sb.Length > 0)
                    SendPing(channel, sb.ToString(), arg);
            }
        }

        private void SendPing(string channel, string names, string message)
        {
            irc.SendMessage(SendType.Message, channel, string.Format("PING:{1} {0}>> {2}{0}", PluginUtils.IrcConstants.IrcBold, names, message));
        }
    }
}
