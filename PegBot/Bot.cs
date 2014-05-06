using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PegBot
{
    class Bot
    {
        private IrcClient irc;
        public readonly string NickName;
        public readonly string UserName;
        
        protected List<string> ConnectedChannels = new List<string>();

        public Bot(string server, int port, string nickname, string username)
        {
            NickName = nickname;
            UserName = username;

            irc = new IrcClient();

            irc.Encoding = System.Text.Encoding.UTF8;
            irc.SendDelay = 200;
            irc.ActiveChannelSyncing = true;

            irc.OnConnected += new EventHandler(OnConnected);
            irc.OnInvite += new InviteEventHandler(OnInvite);
            irc.OnKick += new KickEventHandler(OnKick);
            irc.OnDisconnected += new EventHandler(OnDisconnected);
            irc.OnQueryMessage += new IrcEventHandler(OnQueryMessage);
            irc.OnError += new ErrorEventHandler(OnError);
            irc.OnRawMessage += new IrcEventHandler(OnRawMessage);


            Console.WriteLine(string.Format("Connecting to {0}:{1}...", server, port));

            try
            {
                irc.Connect(server, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on connect: " + ex.Message);
            }
            
        }

        private void OnInvite(object sender, InviteEventArgs e)
        {
            irc.RfcJoin(e.Channel);
            ConnectedChannels.Add(e.Channel);
        }
        
        private void OnKick(object sender, KickEventArgs e)
        {
            ConnectedChannels.Remove(e.Channel);
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            Console.WriteLine("Disconnected.");
        }

        private void OnRawMessage(object sender, IrcEventArgs e)
        {
            string msg = e.Data.Message;
            string nick = e.Data.Nick;
            string channel = e.Data.Channel;

            if (msg != null && msg.StartsWith(".ping"))
            {
                string pingmsg = msg.Substring(5).Trim();
                if(pingmsg.Length > 0) 
                    BotFunctions.Ping(irc, channel, );
                
            }
            Console.WriteLine((string.IsNullOrEmpty(e.Data.Nick) ? "" : e.Data.Nick + ": " ) + e.Data.Message ?? "");
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private void OnQueryMessage(object sender, IrcEventArgs e)
        {
            Console.WriteLine(string.Format("<{0}>: {1}", e.Data.Nick, e.Data.Message));
        }

        private void OnConnected(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("Connected to server.");
                Console.WriteLine("Logging in...");
                irc.Login(NickName, NickName, 4, UserName);

                irc.Listen();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                irc.Disconnect();
            }
        }
    }
}
