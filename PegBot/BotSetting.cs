using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace PegBot
{
    [Serializable()]
    public class BotSetting
    {
        public string FileName;
        public List<PluginSettings> Plugins = new List<PluginSettings>();

        public static BotSetting LoadBotSetting(string fileName)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(BotSetting));
                string f = File.ReadAllText(fileName, Encoding.UTF8);

                using (TextReader reader = new StringReader(f))
                {
                    return (BotSetting)serializer.Deserialize(reader);
                }
            }
            catch
            {
                return new BotSetting(fileName);
            }
        }

        public BotSetting() { }

        public BotSetting(string fileName)
        {
            this.FileName = fileName;
        }

        protected void SaveSettings()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(BotSetting));
            using (var f = File.Create(FileName))
            {
                using (StreamWriter writer = new StreamWriter(f, Encoding.UTF8))
                {
                    serializer.Serialize(writer, this);
                }
            }
        }

        public void SetPluginEnabled(string channel, string plugin, bool enabled)
        {
            PluginSettings pl;
            int plIndex = GetCreatePluginSettings(out pl, plugin);
            pl.SetChannelEnabled(channel, enabled);
            Plugins[plIndex] = pl;

            SaveSettings();
        }

        public void SetPluginSetting(string plugin, string channel, object setting)
        {
            PluginSettings pl;
            int plIndex = GetCreatePluginSettings(out pl, plugin);
            pl.SetChannelSetting(channel, setting);
            Plugins[plIndex] = pl;

            SaveSettings();
        }

        public object GetPluginSetting(string plugin, string channel)
        {
            var x = from xx in Plugins
                    where xx.PluginName == plugin
                    from yy in xx.Channels
                    where yy.ChannelName == channel
                    select yy.Setting;

            return x.FirstOrDefault();
        }

        public bool IsPluginEnabled(string plugin, string channel)
        {
            var x = from xx in Plugins
                    where xx.PluginName.Equals(plugin, StringComparison.CurrentCultureIgnoreCase)
                    from yy in xx.Channels
                    where yy.ChannelName.Equals(channel, StringComparison.CurrentCultureIgnoreCase)
                    select yy.PluginEnabled;

            return x.FirstOrDefault();
        }

        public List<string> GetAllEnabledChannels()
        {
            var ch = from xx in Plugins
                     from yy in xx.Channels
                     where yy.PluginEnabled
                     select yy.ChannelName;

            return ch.Distinct().ToList();
        }

        public List<string> GetEnabledChannels(string plugin)
        {
            var ch = from xx in Plugins
                     where xx.PluginName == plugin
                     from yy in xx.Channels
                     select yy.ChannelName;

            return ch.ToList();
        }

        private int GetCreatePluginSettings(out PluginSettings pluginSettings, string pluginName)
        {
            var pl = Plugins.Select((p, index) => new { p, index }).FirstOrDefault(pi => pi.p.PluginName == pluginName);
            if (pl == null)
            {
                pl = new { p = new PluginSettings(pluginName), index = Plugins.Count() };
                Plugins.Add(pl.p);
            }
            pluginSettings = pl.p;
            return pl.index;
        }

        [Serializable()]
        public class PluginSettings
        {
            public string PluginName;
            public List<PluginChannelSetting> Channels = new List<PluginChannelSetting>();

            public PluginSettings() { }

            public PluginSettings(string pluginName)
            {
                this.PluginName = pluginName;
            }

            public void SetChannelEnabled(string channelName, bool enabled)
            {
                PluginChannelSetting ch;
                int chIndex = GetCreatePluginChannelSettings(out ch, channelName);

                ch.PluginEnabled = enabled;

                Channels[chIndex] = ch;
            }

            public void SetChannelSetting(string channelName, object setting)
            {
                PluginChannelSetting ch;
                int chIndex = GetCreatePluginChannelSettings(out ch, channelName);

                ch.Setting = setting;

                Channels[chIndex] = ch;
            }

            private int GetCreatePluginChannelSettings(out PluginChannelSetting channelSetting, string channelName)
            {
                var ch = Channels.Select((c, index) => new { c, index }).FirstOrDefault(ci => ci.c.ChannelName == channelName);
                if (ch == null)
                {
                    ch = new { c = new PluginChannelSetting(channelName), index = Channels.Count() };
                    Channels.Add(ch.c);
                }
                channelSetting = ch.c;
                return ch.index;
            }
        }

        [Serializable()]
        public class PluginChannelSetting
        {
            public string ChannelName;
            public bool PluginEnabled;
            public string settingString;

            public PluginChannelSetting() { }

            public PluginChannelSetting(string channel)
            {
                ChannelName = channel;
                PluginEnabled = true;
            }

            [XmlIgnore()]
            public object Setting
            {
                get
                {
                    if (string.IsNullOrEmpty(settingString))
                        return null;
                    using (var stream = new MemoryStream(Convert.FromBase64String(settingString)))
                    {
                        return new BinaryFormatter().Deserialize(stream);
                    }

                }
                set
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        new BinaryFormatter().Serialize(stream, value);
                        settingString = Convert.ToBase64String(stream.ToArray());
                    }
                }
            }
        }
    }
}
