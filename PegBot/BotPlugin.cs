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
        private List<string> _EnabledChannels = new List<string>();
        
        protected List<string> EnabledChannels
        {
            get
            {
                return _EnabledChannels;
            }
        }

        public BotPlugin(IrcClient irc, string PluginName)
        {
            this.irc = irc;
            this.PluginName = PluginName;
            System.Console.WriteLine(PluginName + " loaded");
        }

        public bool ChannelEnabled(string channel)
        {
            return EnabledChannels.Exists(c => c.Equals(channel, StringComparison.CurrentCultureIgnoreCase));
        }

        public void EnableChannel(string channel)
        {
            _EnabledChannels.Add(channel);
        }

        public void DisableChannel(string channel)
        {
            _EnabledChannels.Remove(channel);
        }

        public abstract string[] GetHelpCommands();
    }
}
