using Meebey.SmartIrc4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PegBot
{
    static class BotFunctions
    {
        public static void Ping(IrcClient irc, string channel, string message)
        {
            StringBuilder sb = new StringBuilder();
            
            Channel c = irc.GetChannel(channel);
            foreach (DictionaryEntry user in c.Users)
            {
                //skip self
                if (((string)user.Key) == irc.Nickname)
                    continue;
                sb.Append(" ");
                sb.Append(user.Key);
            }

            irc.SendMessage(SendType.Message, channel, "PING:" + sb.ToString());
            irc.SendMessage(SendType.Message, channel, ">>" + message);
        }
    }
}
