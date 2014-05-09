using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PegBot.Plugins;
using System.IO;

namespace PegBot
{
    class Bot
    {
        private IrcClient irc;
        public readonly string NickName;
        public readonly string UserName;
        private List<BotPlugin> Plugins;
        private List<string> AutoJoinChannels;
        private const string FILENAME_AUTOJOINCHANNELS = "autojoin-channels";

        public Bot(string server, int port, string nickname, string username)
        {
            NickName = nickname;
            UserName = username;

            AutoJoinChannels = new List<string>();
            try
            {
                AutoJoinChannels = PluginUtils.LoadObject(FILENAME_AUTOJOINCHANNELS) as List<string>;
            }
            catch (IOException) { }

            irc = new IrcClient();
            irc.Encoding = System.Text.Encoding.UTF8;
            irc.SendDelay = 200;
            irc.ActiveChannelSyncing = true;
            irc.AutoReconnect = true;
            irc.AutoJoinOnInvite = true;
            irc.AutoRejoin = true;
            irc.AutoRejoinOnKick = false;
            irc.AutoRelogin = true;
            irc.AutoRetry = true;
            irc.AutoRetryDelay = 60;

            irc.OnConnected += new EventHandler(OnConnected);
            irc.OnQueryMessage += new IrcEventHandler(OnQueryMessage);
            irc.OnChannelMessage += new IrcEventHandler(OnChannelMessage);
            irc.OnInvite += OnInvite;
            irc.OnKick += OnKick;
            irc.OnRegistered += OnRegistered;

            LoadPlugins();

            try
            {
                Console.WriteLine(string.Format("Connecting to {0}:{1}...", server, port));
                irc.Connect(server, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on connect: " + ex.Message);
            }
        }

        void OnRegistered(object sender, EventArgs e)
        {
            foreach (string channel in AutoJoinChannels)
                irc.RfcJoin(channel);
        }

        void OnKick(object sender, KickEventArgs e)
        {
            AutoJoinChannels.Remove(e.Channel);
            PluginUtils.SaveObject(AutoJoinChannels, FILENAME_AUTOJOINCHANNELS);
        }

        void OnInvite(object sender, InviteEventArgs e)
        {
            if(!AutoJoinChannels.Exists(c => c == e.Channel))
            {
                AutoJoinChannels.Add(e.Channel);
                PluginUtils.SaveObject(AutoJoinChannels, FILENAME_AUTOJOINCHANNELS);
            }
        }

        private void LoadPlugins()
        {
            Plugins = new List<BotPlugin>()
                { 
                    new PingPlugin(irc),
                    new URLTitlePlugin(irc),
                    new HLTVWatcher(irc)
                };
        }

        private void OnConnected(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("Connected to server.");
                irc.Login(NickName, NickName, 4, UserName);
                irc.Listen();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                irc.Disconnect();
            }
        }

        private void OnChannelMessage(object sender, IrcEventArgs e)
        {
            string message = e.Data.Message.Trim();
            ParseChannelMessage(message, e.Data.Channel, e.Data.Nick, e.Data.Channel);
        }

        private void OnQueryMessage(object sender, IrcEventArgs e)
        {
            string channel = e.Data.Message.Split(' ').Last();
            string message = e.Data.Message.Substring(0, e.Data.Channel.Length - channel.Length).Trim();
            ParseChannelMessage(message, channel, e.Data.Nick, e.Data.Nick);
        }

        private void ParseChannelMessage(string message, string channel, string nick, string replyDestination)
        {
            if (message.Equals(".help", StringComparison.CurrentCultureIgnoreCase))
            {
                irc.SendMessage(SendType.Message, replyDestination, ".plugin list -- List all plugins");
                irc.SendMessage(SendType.Message, replyDestination, ".plugin <enable/disable> <plugin> -- Enable/disable plugin");
                if(nick == replyDestination)
                    irc.SendMessage(SendType.Message, replyDestination, "Add #<channel> to apply onto a specific channel");
                foreach (BotPlugin p in Plugins)
                    if (p.ChannelEnabled(channel))
                        foreach (string text in p.GetHelpCommands())
                            irc.SendMessage(SendType.Message, replyDestination, text);
            }

            if (message.Equals(".plugin list", StringComparison.CurrentCultureIgnoreCase))
            {
                irc.SendMessage(SendType.Message, channel, "[X] => enabled in " + channel);
                foreach (BotPlugin p in Plugins)
                    irc.SendMessage(SendType.Message, replyDestination, string.Format("[{0}] {1}", p.ChannelEnabled(channel) ? 'X' : ' ', p.PluginName));
            }

            string pluginName = message.Split(' ').Last();
            message = message.Substring(0, message.Length - pluginName.Length).Trim();
            BotPlugin plugin = Plugins.Find(p => p.PluginName.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase));

            if(message.Equals(".plugin enable", StringComparison.CurrentCultureIgnoreCase))
            {
                if(plugin == null)
                    irc.SendMessage(SendType.Message, replyDestination, "Found no plugin named " + pluginName);
                else if(plugin.PluginEnabler(true, channel, irc.GetChannelUser(channel, nick)))
                    irc.SendMessage(SendType.Message, replyDestination, "Only a channel operator can enable or disable plugins");
            }

            if (message.Equals(".plugin disable", StringComparison.CurrentCultureIgnoreCase))
            {
                if (plugin == null)
                    irc.SendMessage(SendType.Message, replyDestination, "Found no plugin named " + pluginName);
                else if (plugin.PluginEnabler(false, channel, irc.GetChannelUser(channel, nick)))
                    irc.SendMessage(SendType.Message, replyDestination, "Only a channel operator can enable or disable plugins");
            }
        }
    }
}