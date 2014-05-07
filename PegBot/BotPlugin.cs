using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;

namespace PegBot
{
    abstract class BotPlugin
    {
        protected IrcClient irc;
        public readonly string PluginName;

        public BotPlugin(IrcClient irc, string PluginName)
        {
            this.irc = irc;
            this.PluginName = PluginName;
            System.Console.WriteLine(PluginName + " loaded");
        }

        public abstract string[] GetHelpCommands();
    }
}
