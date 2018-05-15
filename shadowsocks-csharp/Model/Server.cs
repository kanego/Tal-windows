using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using Shadowsocks.Controller;
using System.Text.RegularExpressions;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Server
    {
        public string server;
        public int server_port;

        public override int GetHashCode()
        {
            return server.GetHashCode() ^ server_port;
        }

        public override bool Equals(object obj)
        {
            Server o2 = (Server)obj;
            return server == o2.server && server_port == o2.server_port;
        }

        public string FormatHostName(string hostName)
        {
            // CheckHostName() won't do a real DNS lookup
            switch (Uri.CheckHostName(hostName))
            {
                case UriHostNameType.IPv6:  // Add square bracket when IPv6 (RFC3986)
                    return $"[{hostName}]";
                default:    // IPv4 or domain name
                    return hostName;
            }
        }

        public Server()
        {
            server = "baidu.com";
            server_port = 8388;
        }
        public string Identifier()
        {
            return server + ':' + server_port;
        }
    }
}
