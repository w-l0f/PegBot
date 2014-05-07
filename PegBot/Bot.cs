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

            Plugins = new BotPlugin[] {new PingPlugin(irc), new HLTVWatcher(irc)};

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
            if (e.Data.Message != null && e.Data.Message.StartsWith(".help"))
                PrintHelp(e.Data.Channel);
        }

        private void OnQueryMessage(object sender, IrcEventArgs e)
        {
            if (e.Data.Message != null && e.Data.Message.StartsWith(".help"))
                PrintHelp(e.Data.From);
        }

        private void PrintHelp(string destination)
        {
            StringBuilder sb = new StringBuilder(); 
            foreach(BotPlugin plugin in Plugins)
            {
                foreach(string command in plugin.GetHelpCommands())
                {
                    sb.Append(command);
                }
            }
            irc.SendMessage(SendType.Message, destination, sb.ToString());
        }
    }
}
