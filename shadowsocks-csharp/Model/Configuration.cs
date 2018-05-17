using System;
using System.Collections.Generic;
using System.IO;

using Shadowsocks.Controller;
using Newtonsoft.Json;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Configuration
    {
        public List<Server> configs;
        public int index;
        public string reporturl;

        private static string CONFIG_FILE = "config.json";

        public Server GetCurrentServer()
        {
            //if (index >= 0 && index < configs.Count)
            //    return configs[index];
            //else
            return GetDefaultServer();
        }

        public static void CheckServer(Server server)
        {
            CheckServer(server.server);
        }

        public static Configuration Load()
        {
            try
            {
                string configContent = File.ReadAllText(CONFIG_FILE);
                Configuration config = JsonConvert.DeserializeObject<Configuration>(configContent);

                if (config.configs == null)
                    config.configs = new List<Server>();
                if (config.configs.Count == 0)
                    config.configs.Add(GetDefaultServer());

                return config;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                    Logging.LogUsefulException(e);
                return new Configuration
                {
                    configs = new List<Server>()
                    {
                        GetDefaultServer()
                    },
                };
            }
        }

        public static void Save(Configuration config)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(CONFIG_FILE, FileMode.Create)))
                {
                    string jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
                    sw.Write(jsonString);
                    sw.Flush();
                }
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public static Server GetDefaultServer()
        {
            return new Server();
        }

        private static void Assert(bool condition)
        {
            if (!condition)
                throw new Exception(I18N.GetString("assertion failure"));
        }
        public static void CheckServer(string server)
        {
            if (server.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Server IP can not be blank"));
        }

        public static void CheckTimeout(int timeout, int maxTimeout)
        {
            if (timeout <= 0 || timeout > maxTimeout)
                throw new ArgumentException(string.Format(
                    I18N.GetString("Timeout is invalid, it should not exceed {0}"), maxTimeout));
        }
    }
}
