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

        public BotPlugin(IrcClient irc, string PluginName)
        {
            this.irc = irc;
            this.PluginName = PluginName;
            if(PluginName.Contains(' '))
                throw new ArgumentException("PluginName can't contain any spaces");

            System.Console.WriteLine(PluginName + " loaded");
        }

        public List<string> EnabledChannels
        {
            get
            {
                return Bot.Setting.GetEnabledChannels(PluginName);
            }
        }

        public bool ChannelEnabled(string channel)
        {
            return Bot.Setting.IsPluginEnabled(PluginName, channel);
        }

        public object GetSetting(string channel)
        {
            return Bot.Setting.GetPluginSetting(PluginName, channel);
        }

        public void SetSetting(string channel, object setting)
        {
            Bot.Setting.SetPluginSetting(PluginName, channel, setting);
        }

        public abstract string[] GetHelpCommands();
    }
}
