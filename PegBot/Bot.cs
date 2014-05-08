using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PegBot.Plugins;

namespace PegBot
{
    class Bot
    {
        private IrcClient irc;
        public readonly string NickName;
        public readonly string UserName;
        private BotPlugin[] Plugins;
        private enum PluginAction { Enable, Disable };

        public Bot(string server, int port, string nickname, string username)
        {
            NickName = nickname;
            UserName = username;

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

            LoadPlugins();

            irc.OnConnected += new EventHandler(OnConnected);
            irc.OnQueryMessage += new IrcEventHandler(OnQueryMessage);
            irc.OnChannelMessage += new IrcEventHandler(OnChannelMessage);

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

        private void LoadPlugins()
        {
            Plugins = new BotPlugin[] 
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
            string message = e.Data.Message;
            if (!string.IsNullOrWhiteSpace(message))
            {
                if (message.StartsWith(".help", StringComparison.CurrentCultureIgnoreCase))
                {
                    PrintHelp(e.Data.Channel);
                }
                else if (message.StartsWith(".plugin ", StringComparison.CurrentCultureIgnoreCase))
                {
                    string[] split = message.Split(' ');
                    if (split.Length >= 3)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 2; i < split.Length; i++)
                            sb.Append(split[i]);

                        if (split[1].Equals("enable", StringComparison.CurrentCultureIgnoreCase))
                            EnablePlugin(PluginAction.Enable, e.Data.Nick, e.Data.Channel, sb.ToString());
                        else if (split[1].Equals("disable", StringComparison.CurrentCultureIgnoreCase))
                            EnablePlugin(PluginAction.Disable, e.Data.Nick, e.Data.Channel, sb.ToString());
                    }
                    else if (split.Length >= 2 && split[1].Equals("list", StringComparison.CurrentCultureIgnoreCase))
                    {
                        ListPlugins(e.Data.Channel);
                    }
                }
            }
        }

        private void OnQueryMessage(object sender, IrcEventArgs e)
        {
            var message = e.Data.Message;
            if (!string.IsNullOrWhiteSpace(message))
            {
                if (message.StartsWith(".help"))
                    PrintHelp(e.Data.Nick);
                else if (message.StartsWith(".plugin list"))
                {
                    ListPluginsQuery(e.Data.Nick);
                }
            }
        }

        private void ListPlugins(string channel)
        {
            irc.SendMessage(SendType.Message, channel, "[X] => enabled in " + channel);
            irc.SendMessage(SendType.Message, channel, "===");
            foreach (var plugin in Plugins)
            {
                irc.SendMessage(SendType.Message, channel,
                    string.Format("[{0}] {1}", plugin.ChannelEnabled(channel) ? 'X' : ' ', plugin.PluginName));
            }
        }

        private void ListPluginsQuery(string nick)
        {
            irc.SendMessage(SendType.Message, nick, "Availiable plugins");
            foreach (var plugin in Plugins)
            {
                irc.SendMessage(SendType.Message, nick, plugin.PluginName);
            }
        }

        private List<string> HelpTextHeader = new List<string>
        {
            ".plugin list -- List all plugins",
            ".plugin <enable/disable> <plugin> -- Enable/disable plugin",
            "=== Plugin commands"
        };

        private void PrintHelp(string destination)
        {
            HelpTextHeader.ForEach(m => irc.SendMessage(SendType.Message, destination, m));

            foreach (BotPlugin plugin in Plugins)
            {
                foreach (string command in plugin.GetHelpCommands())
                {
                    irc.SendMessage(SendType.Message, destination, command);
                }
            }
        }

        private void EnablePlugin(PluginAction action, string nick, string channel, string plugin)
        {
            var user = irc.GetChannelUser(channel, nick);
            if (user.IsOp)
            {
                var p = Plugins.FirstOrDefault(x => x.PluginName.Equals(plugin.Trim(), StringComparison.CurrentCultureIgnoreCase));
                if (p != null)
                {
                    if (action == PluginAction.Enable)
                        p.EnableChannel(channel);
                    else
                        p.DisableChannel(channel);

                    irc.SendMessage(SendType.Message, channel, action + "d " + p.PluginName);
                }
                else
                    irc.SendMessage(SendType.Message, channel, "Found no plugin named " + plugin);
            }
            else
                irc.SendMessage(SendType.Message, channel, "Only a channel operator can enable or disable plugins");
        }
    }
}
