using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PegBot.Plugins
{
    class RandomPlugin : BotPlugin
    {
        Random rnd;
        public RandomPlugin(IrcClient irc)
            : base(irc, "random")
        {
            rnd = new Random();
            RegisterCommand(".roll", "[max]", "Roll a random number between 1 and 100 or [max]", OnRoll, arg => String.IsNullOrEmpty(arg) || Regex.IsMatch(arg, @"^\d+$"), false);
            RegisterCommand(".magic8ball", "<question>", "Ask question to the magic 8 ball", OnMagic, false);
        }

        private void OnRoll(string arg, string channel, string nick, string replyTo)
        {
            int roll = String.IsNullOrEmpty(arg) ? rnd.Next(100) + 1 : rnd.Next(Int32.Parse(arg)) + 1; //they see me rollin
            irc.SendMessage(SendType.Message, replyTo, nick + ": " + roll);
        }

        private void OnMagic(string arg, string channel, string nick, string replyTo)
        {
            string[] anwsers = {
                "It is certain",
                "It is decidedly so",
                "Without a doubt",
                "Yes definitely",
                "You may rely on it",
                "As I see it, yes",
                "Most likely",
                "Outlook good",
                "Yes",
                "Signs point to yes",
                "Reply hazy try again",
                "Ask again later",
                "Better not tell you now",
                "Cannot predict now",
                "Concentrate and ask again",
                "Don't count on it",
                "My reply is no",
                "My sources say no",
                "Outlook not so good",
                "Very doubtful"};
            irc.SendMessage(SendType.Message, replyTo, nick + ": " + anwsers[rnd.Next(anwsers.Length)]);
        }
    }
}
