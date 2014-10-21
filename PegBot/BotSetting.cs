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
            PluginSettings pl = GetCreatePluginSettings(plugin);
            pl.SetChannelEnabled(channel, enabled);

            SaveSettings();
        }

        public void SetPluginSetting(string plugin, string channel, object setting)
        {
            PluginSettings pl = GetCreatePluginSettings(plugin);
            pl.SetChannelSetting(channel, setting);

            SaveSettings();
        }

        public object GetPluginSetting(string plugin, string channel)
        {
            var x = from xx in Plugins
                    where xx.PluginName.Equals(plugin, StringComparison.CurrentCultureIgnoreCase)
                    from yy in xx.Channels
                    where yy.ChannelName.Equals(channel, StringComparison.CurrentCultureIgnoreCase)
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
                     where xx.PluginName.Equals(plugin, StringComparison.CurrentCultureIgnoreCase)
                     from yy in xx.Channels
                     where yy.PluginEnabled
                     select yy.ChannelName;

            return ch.ToList();
        }

        private PluginSettings GetCreatePluginSettings(string pluginName)
        {
            var pl = Plugins.FirstOrDefault(p => p.PluginName.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase));
            if (pl == null)
            {
                pl = new PluginSettings(pluginName);
                Plugins.Add(pl);
            }

            return pl;
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
                PluginChannelSetting ch = GetCreatePluginChannelSettings(channelName);
                ch.PluginEnabled = enabled;
            }

            public void SetChannelSetting(string channelName, object setting)
            {
                PluginChannelSetting ch = GetCreatePluginChannelSettings(channelName);
                ch.Setting = setting;
            }

            private PluginChannelSetting GetCreatePluginChannelSettings(string channelName)
            {
                var ch = Channels.FirstOrDefault(c => c.ChannelName.Equals(channelName, StringComparison.CurrentCultureIgnoreCase));
                if (ch == null)
                {
                    ch = new PluginChannelSetting(channelName);
                    Channels.Add(ch);
                }

                return ch;
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
