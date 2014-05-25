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
    class Bot : BotPlugin
    {
        public readonly string NickName;
        public readonly string UserName;
        public readonly string Server;
        
        private List<BotPlugin> Plugins;

        public Bot(string server, int port, string nickname, string username)
            : base(new IrcClient(), "MainPlugin")
        {
            NickName = nickname;
            UserName = username;
            Server = server;

            Setting = BotSetting.LoadBotSetting(server.Replace('.', '_') + ".xml");

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
            irc.OnInvite += OnInvite;
            irc.OnKick += OnKick;
            irc.OnRegistered += OnRegistered;

            LoadPlugins();

            RegisterExactCommand(".help", "This help message", OnHelp, false);
            RegisterCommand(".plugin list", "List all plugins", OnPluginList, false);
            RegisterCommand(".plugin enable", "<plugin>", "Enable plugin", OnPluginEnable);
            RegisterCommand(".plugin disable", "<plugin>", "Disable plugin", OnPluginEnable);

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
            foreach (string channel in EnabledChannels)
                irc.RfcJoin(channel);
        }

        void OnKick(object sender, KickEventArgs e)
        {
            Setting.SetPluginEnabled(e.Channel, PluginName, false);
        }

        void OnInvite(object sender, InviteEventArgs e)
        {
            Setting.SetPluginEnabled(e.Channel, PluginName, true);
        }

        private void LoadPlugins()
        {
            Plugins = new List<BotPlugin>()
                { 
                    new PingPlugin(irc),
                    new URLTitlePlugin(irc),
                    new HLTVWatcher(irc),
                    new TwitchPlugin(irc),
                    new RSSReader(irc),
                    new RandomPlugin(irc),
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
                Console.WriteLine(ex.StackTrace);
                irc.Disconnect();
            }
        }

        private void OnHelp(string arg, string channel, string nick, string replyTo)
        {
            if (nick == replyTo)
                irc.SendMessage(SendType.Message, replyTo, "Add #<channel> to apply onto a specific channel");
            irc.SendMessage(SendType.Message, replyTo, ".plugin list -- List all plugins");
            irc.SendMessage(SendType.Message, replyTo, ".plugin <enable/disable> <plugin> -- Enable/disable plugin");
            foreach (BotPlugin p in Plugins)
                foreach (string text in p.GetHelpCommands(channel))
                    irc.SendMessage(SendType.Message, replyTo, text);
        }

        private void OnPluginList(string arg, string channel, string nick, string replyTo)
        {
            irc.SendMessage(SendType.Message, replyTo, "[X] => enabled in " + channel);
            foreach (BotPlugin p in Plugins)
                irc.SendMessage(SendType.Message, replyTo, string.Format("[{0}] {1}", p.ChannelEnabled(channel) ? 'X' : ' ', p.PluginName));
        }

        private void OnPluginEnable(string arg, string channel, string nick, string replyTo)
        {
            BotPlugin plugin = Plugins.Find(p => p.PluginName.Equals(arg, StringComparison.CurrentCultureIgnoreCase));
            if(plugin == null)
                irc.SendMessage(SendType.Message, replyTo, "Found no plugin named " + arg);
            else
                Setting.SetPluginEnabled(channel, plugin.PluginName, true);
        }

        private void OnPluginDisable(string arg, string channel, string nick, string replyTo)
        {
            BotPlugin plugin = Plugins.Find(p => p.PluginName.Equals(arg, StringComparison.CurrentCultureIgnoreCase));
            if (plugin == null)
                irc.SendMessage(SendType.Message, replyTo, "Found no plugin named " + arg);
            else
                Setting.SetPluginEnabled(channel, plugin.PluginName, false);
        }
    }
}