﻿using System;
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

        public delegate void Handle(string arg, string channel, string nick, string replyTo);
        private List<BotCommandFunction> SubscribedCommands;

        public BotPlugin(IrcClient irc, string PluginName)
        {
            this.irc = irc;
            this.PluginName = PluginName;
            if (PluginName.Contains(' '))
                throw new ArgumentException("PluginName can't contain any spaces");

            SubscribedCommands = new List<BotCommandFunction>();
            irc.OnQueryMessage += OnSubscribedQuery;
            irc.OnChannelMessage += OnSubscribedChannel;
            SubscribeExact(".help", "Print this help info", OnHelp, false);
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

        private void OnHelp(string arg, string channel, string nick, string replyTo)
        {
            if(ChannelEnabled(channel))
                foreach (BotCommandFunction commandFunction in SubscribedCommands)
                    if(commandFunction.command != ".help")
                        irc.SendMessage(SendType.Message, replyTo, commandFunction.ToString());
        }

        public void SubscribeExact(string command, string description, Handle function, bool onlyOp = true)
        {
            Subscribe(command, String.Empty, description, function, a => true, t => t.Equals(command, StringComparison.CurrentCultureIgnoreCase), onlyOp);
        }

        public void Subscribe(string command, string description, Handle function, bool onlyOp = true)
        {
            Subscribe(command, String.Empty, description, function, a => String.IsNullOrEmpty(a), onlyOp);
        }

        public void Subscribe(string command, string args, string description, Handle function, bool onlyOp = true)
        {
            Subscribe(command, args, description, function, a => !String.IsNullOrEmpty(a), onlyOp);
        }

        public void Subscribe(string command, string args, string description, Handle function, Predicate<string> argCheck, bool onlyOp = true)
        {
            Subscribe(command, args, description, function, argCheck, t => t.StartsWith(command, StringComparison.CurrentCultureIgnoreCase) && (t.Length == command.Length || t[command.Length] == ' '), onlyOp);
        }

        public void Subscribe(string command, string args, string description, Handle function, Predicate<string> argCheck, Predicate<string> trigger, bool onlyOp = true)
        {
            SubscribedCommands.Add(new BotCommandFunction(command, args, description, function, argCheck, trigger, onlyOp));
        }

        public void OnSubscribedQuery(object sender, IrcEventArgs e)
        {
            string channel = e.Data.Message.Split(' ').Last();
            string message = e.Data.Message.Substring(0, e.Data.Message.Length - channel.Length).Trim();
            OnSubscribedCommand(message, channel, e.Data.Nick, e.Data.Nick);
        }

        public void OnSubscribedChannel(object sender, IrcEventArgs e)
        {
            OnSubscribedCommand(e.Data.Message.Trim(), e.Data.Channel, e.Data.Nick, e.Data.Channel);
        }

        public void OnSubscribedCommand(string message, string channel, string nick, string replyTo)
        {
            foreach (BotCommandFunction commandFunction in SubscribedCommands.FindAll(c => c.trigger(message)))
            {
                if (String.IsNullOrEmpty(channel) || !channel.StartsWith("#"))
                {
                    irc.SendMessage(SendType.Message, replyTo, "Channel must be specified at end of command and start with a #");
                    break;
                }
                if (!ChannelEnabled(channel))
                    continue;
                if (commandFunction.onlyOp)
                {
                    ChannelUser user = irc.GetChannelUser(channel, nick);
                    if (!user.IsOp)
                    {
                        irc.SendMessage(SendType.Message, replyTo, "Only a channel operator in " + channel + " can use that command");
                        continue;
                    }
                }
                string arg = message.Substring(commandFunction.command.Length).Trim();
                if (!commandFunction.argCheck(arg))
                {
                    irc.SendMessage(SendType.Message, replyTo, "Invalid arguments, " + commandFunction.ToString());
                    continue;
                }
                commandFunction.function(arg, channel, nick, replyTo);
            }
        }

        class BotCommandFunction
        {
            public readonly string command;
            public readonly string args;
            public readonly string description;
            public readonly Handle function;
            public readonly Predicate<string> trigger;
            public readonly Predicate<string> argCheck;
            public readonly bool onlyOp;
            public BotCommandFunction(string command, string args, string description, Handle function, Predicate<string> argCheck, Predicate<string> trigger, bool onlyOp)
            {
                this.command = command;
                this.args = args;
                this.description = description;
                this.function = function;
                this.trigger = trigger;
                this.argCheck = argCheck;
                this.onlyOp = onlyOp;
            }
            public override string ToString()
            {
                return command + " " + args + " -- " + description;
            }
        }
    }
}
