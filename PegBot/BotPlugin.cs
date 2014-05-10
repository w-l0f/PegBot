using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;
using System.IO;

namespace PegBot
{
    abstract class BotPlugin
    {
        protected IrcClient irc;
        public readonly string PluginName;
        private List<string> _EnabledChannels = new List<string>();
        private readonly string FILENAME_ENABLEDCHANNELS;

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
            FILENAME_ENABLEDCHANNELS = PluginName + "-channels";
            if (PluginName.Contains(' '))
                throw new ArgumentException("PluginName can't contain any spaces");
            try
            {
                _EnabledChannels = PluginUtils.LoadObject(FILENAME_ENABLEDCHANNELS) as List<string>;
            }
            catch (IOException) { }
            System.Console.WriteLine(PluginName + " loaded");
        }

        public bool PluginEnabler(bool enable, string channel, ChannelUser user = null)
        {
            if (user != null && !user.IsOp)
                return true;
            if (enable && !ChannelEnabled(channel))
                _EnabledChannels.Add(channel);
            if (!enable && ChannelEnabled(channel))
                _EnabledChannels.Remove(channel);
            PluginUtils.SaveObject(_EnabledChannels, FILENAME_ENABLEDCHANNELS);
            return false;
        }

        public bool ChannelEnabled(string channel)
        {
            return EnabledChannels.Exists(c => c.Equals(channel, StringComparison.CurrentCultureIgnoreCase));
        }

        public abstract string[] GetHelpCommands();
    }
}
