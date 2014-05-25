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
            : base(irc, "Ping")
        {
            RegisterCommand(".ping", "[msg]", "Hilights everyone in channel with specified message", OnPing, arg => true, false);
        }

        private void OnPing(string arg, string channel, string nick, string replyTo)
        {
            StringBuilder sb = new StringBuilder();
            foreach (DictionaryEntry user in irc.GetChannel(channel).Users)
            {
                //skip self
                if (((string)user.Key) == irc.Nickname)
                    continue;
                sb.Append(" ");
                sb.Append(user.Key);
            }
            irc.SendMessage(SendType.Message, channel, "PING:" + sb.ToString());
            if(!String.IsNullOrEmpty(arg))
                irc.SendMessage(SendType.Message, channel, string.Format("{0}>>{0} {1}", PluginUtils.IrcConstants.IrcBold, arg));
        }
    }
}
