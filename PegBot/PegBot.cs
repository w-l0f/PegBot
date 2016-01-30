using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PegBot
{
    class PegBot
    {
        private static string server;
        private static int? port = new int?();
        private static string nickname;
        private static string username;
        private static bool ssl = false;

        static void Main(string[] args)
        {
            HandleArgs(args);
            PluginUtils.Log(DateTime.Now.ToString("U"));
            PluginUtils.Log("Starting in: " + Directory.GetCurrentDirectory());
            new Bot(server, port ?? 6667, nickname ?? "PegBot", username, ssl);
            Console.Write("Press any key to exit");
            Console.Read();
        }

        private static void HandleArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-s":
                        server = args[i + 1];
                        i++;
                        break;
                    case "-p":
                        int p;
                        if (int.TryParse(args[i + 1], out p))
                            port = p;
                        i++;
                        break;
                    case "-n":
                        nickname = args[i + 1];
                        i++;
                        break;
                    case "-u":
                        username = args[i + 1];
                        i++;
                        break;
                    case "-ssl":
                        ssl = true;
                        break;
                }
            }
        }
    }
}
