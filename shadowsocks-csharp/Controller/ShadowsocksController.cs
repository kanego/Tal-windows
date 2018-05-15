using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using Newtonsoft.Json;

using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System.Linq;
using Shadowsocks.Controller.Service;
namespace Shadowsocks.Controller
{
    public class ShadowsocksController
    {
        // controller:
        // handle user actions
        // manipulates UI
        // interacts with low level logic

        private Thread _ramThread;

        private Configuration _config;

        private PingService _pingservice;


        private bool stopped = false;

        public class PathEventArgs : EventArgs
        {
            public string Path;
        }
        public event ErrorEventHandler Errored;

        public ShadowsocksController()
        {
            _config = Configuration.Load();
            _pingservice = new PingService();

            StartReleasingMemory();
        }

        public void Start()
        {
            Reload();
        }

        protected void ReportError(Exception e)
        {
            if (Errored != null)
            {
                Errored(this, new ErrorEventArgs(e));
            }
        }

        public Server GetCurrentServer()
        {
            return _config.GetCurrentServer();
        }

        // always return copy
        public Configuration GetConfigurationCopy()
        {
            return Configuration.Load();
        }

        // always return current instance
        public Configuration GetCurrentConfiguration()
        {
            return _config;
        }
        public void SaveServers(List<Server> servers)
        {
            _config.configs = servers;
            Configuration.Save(_config);
        }
        public void SelectServerIndex(int index)
        {
            _config.index = index;
            SaveConfig(_config);
        }
        public  void SaveConfig(Configuration newConfig)
        {
            Configuration.Save(newConfig);
            Reload();
        }
        public void Stop()
        {
            if (stopped)
            {
                return;
            }
            stopped = true;
        }

        public string GetServerURLForCurrentServer()
        {
            Server server = GetCurrentServer();
            return GetServerURL(server);
        }

        public static string GetServerURL(Server server)
        {
            string tag = string.Empty;
            string url = string.Empty;
            return server.server;
        }
        protected void Reload()
        {
            // some logic in configuration updated the config when saving, we need to read it again
            _config = Configuration.Load();
            _pingservice.Stop();
            _pingservice.Start(_config);  
            Utils.ReleaseMemory(true);
        }

        private void StartReleasingMemory()
        {
            _ramThread = new Thread(new ThreadStart(ReleaseMemory));
            _ramThread.IsBackground = true;
            _ramThread.Start();
        }

        private void ReleaseMemory()
        {
            while (true)
            {
                Utils.ReleaseMemory(false);
                Thread.Sleep(30 * 1000);
            }
        }
    }
}
