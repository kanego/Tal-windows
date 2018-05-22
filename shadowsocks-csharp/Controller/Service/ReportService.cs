using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using Shadowsocks.Model;
using Shadowsocks.Util;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Management;

namespace Shadowsocks.Controller.Service
{
    class ReportService
    {
        private readonly TimeSpan _delayBeforeStart = TimeSpan.FromSeconds(1);
        private readonly int kRepeatPingTimes = 10;
        private Configuration _config;
        private MySystemInfo _sysinfo;
        private Timer _pingtimer;
        private Timer _systimer;
        private readonly TimeSpan _pingInterval = TimeSpan.FromSeconds(60);
        private readonly TimeSpan _sysInterval = TimeSpan.FromSeconds(60);
        private string _mac;
        private string _reporturl;
        public ReportService()
        {
            _sysinfo = new MySystemInfo();
            _mac = Utils.GetMacAddress();
            _config = Configuration.Load();
            _reporturl = _config.reporturl;
            StartTimerWithoutState(ref _systimer, RunSys, _sysInterval);
        }
        public void Start(Configuration newConfig)
        {
            _config = newConfig;
            StartTimerWithoutState(ref _pingtimer, RunPing, _pingInterval);
        }
        public void Stop()
        {
            _pingtimer?.Dispose();
        }
        private void StartTimerWithoutState(ref Timer timer, TimerCallback callback, TimeSpan interval)
        {
            timer = new Timer(callback, null, _delayBeforeStart, interval);
        }
        private void RunPing(object _)
        {
            foreach (Server server in _config.configs)
            {
                MyPing ping = new MyPing(server, kRepeatPingTimes);
                ping.Completed += ping_Completed;
                ping.Start();
            }

        }
        private void RunSys(object _)
        {
            SysData sysdata = new SysData();
            sysdata.mac = _mac;
            sysdata.cpu = _sysinfo.CpuLoad; ;
            sysdata.memory = _sysinfo.MemoryRate;

            string jsonString = JsonConvert.SerializeObject(sysdata, Formatting.Indented);

            Logging.Info($"sysinfo{jsonString}");

            string result = Utils.HttpPost(_reporturl, jsonString);

            Logging.Info($"sysinfo report result: {result}");

        }
        private void ping_Completed(object sender, MyPing.CompletedEventArgs e)
        {
            PingData pingdata = new PingData();
            pingdata.mac = _mac;
            pingdata.host = e.Server.server;
            pingdata.ip = e.ip;
            int lateTotal =0;
            foreach (int late in e.RoundtripTime)
            {
                lateTotal += late;
            }

            float averagelate = 0.0f;
            if (e.pingTimes == e.faildePings)
            {
                averagelate = 2000;
            }
            else
            {
                averagelate = lateTotal / (e.pingTimes - e.faildePings);
            }
            pingdata.delay = Convert.ToInt32(averagelate);
            pingdata.loss = Convert.ToInt32((float)e.faildePings / e.pingTimes * 100) + "%";
            string jsonString = JsonConvert.SerializeObject(pingdata, Formatting.Indented);

            Logging.Info($"networkinfo {jsonString}");

            string result = Utils.HttpPost(_reporturl, jsonString);

            Logging.Info($"networkinfo report result: {result}");

        }
        class PingData
        {
            public string mac;
            public string loss;
            public int delay;
            public string ip;
            public string host;
            public int type = 1;
        }
        class SysData
        {
            public string mac;
            public string cpu;
            public string memory;
            public int type = 0;
        }
        public class MySystemInfo
        {
            private int m_ProcessorCount = 0;   //CPU个数
            private PerformanceCounter pcCpuLoad;   //CPU计数器
            private long m_PhysicalMemory = 0;   //物理内存

            private const int GW_HWNDFIRST = 0;
            private const int GW_HWNDNEXT = 2;
            private const int GWL_STYLE = (-16);
            private const int WS_VISIBLE = 268435456;
            private const int WS_BORDER = 8388608;

            #region AIP声明
            [DllImport("IpHlpApi.dll")]
            extern static public uint GetIfTable(byte[] pIfTable, ref uint pdwSize, bool bOrder);

            [DllImport("User32")]
            private extern static int GetWindow(int hWnd, int wCmd);

            [DllImport("User32")]
            private extern static int GetWindowLongA(int hWnd, int wIndx);

            [DllImport("user32.dll")]
            private static extern bool GetWindowText(int hWnd, StringBuilder title, int maxBufSize);

            [DllImport("user32", CharSet = CharSet.Auto)]
            private extern static int GetWindowTextLength(IntPtr hWnd);
            #endregion

            #region 构造函数
            /// <summary>
            /// 构造函数，初始化计数器等
            /// </summary>
            public MySystemInfo()
            {
                //初始化CPU计数器
                pcCpuLoad = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                pcCpuLoad.MachineName = ".";
                pcCpuLoad.NextValue();

                //CPU个数
                m_ProcessorCount = Environment.ProcessorCount;

                //获得物理内存
                ManagementClass mc = new ManagementClass("Win32_ComputerSystem");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if (mo["TotalPhysicalMemory"] != null)
                    {
                        m_PhysicalMemory = long.Parse(mo["TotalPhysicalMemory"].ToString());
                    }
                }
            }
            #endregion

            #region CPU个数
            /// <summary>
            /// 获取CPU个数
            /// </summary>
            public int ProcessorCount
            {
                get
                {
                    return m_ProcessorCount;
                }
            }
            #endregion

            #region CPU占用率
            /// <summary>
            /// 获取CPU占用率
            /// </summary>
            public string CpuLoad
            {
                get
                {
                    return Convert.ToInt32(pcCpuLoad.NextValue()) +"%";
                }
            }
            #endregion

            #region 可用内存
            /// <summary>
            /// 获取可用内存
            /// </summary>
            public long MemoryAvailable
            {
                get
                {
                    long availablebytes = 0;
                    //ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_PerfRawData_PerfOS_Memory");
                    //foreach (ManagementObject mo in mos.Get())
                    //{
                    //    availablebytes = long.Parse(mo["Availablebytes"].ToString());
                    //}
                    ManagementClass mos = new ManagementClass("Win32_OperatingSystem");
                    foreach (ManagementObject mo in mos.GetInstances())
                    {
                        if (mo["FreePhysicalMemory"] != null)
                        {
                            availablebytes = 1024 * long.Parse(mo["FreePhysicalMemory"].ToString());
                        }
                    }
                    return availablebytes;
                }
            }
            #endregion

            #region 物理内存
            /// <summary>
            /// 获取物理内存
            /// </summary>
            public long PhysicalMemory
            {
                get
                {
                    return m_PhysicalMemory;
                }
            }
            #endregion

            #region 内存使用率
            /// <summary>
            /// 获取物理内存
            /// </summary>
            public string MemoryRate
            {
                get
                {
                    return Convert.ToInt32((float)(m_PhysicalMemory-MemoryAvailable) / m_PhysicalMemory * 100) + "%";
                }
            }
            #endregion
        }

        public class MyPing
        {
            //arguments for ICMP tests
            public const int TimeoutMilliseconds = 500;

            public EventHandler<CompletedEventArgs> Completed;
            private Server server;

            private int repeat;
            private int failedpings;
            private int pingTimes;
            private IPAddress ip;
            private Ping ping;
            private List<int?> RoundtripTime;

            public MyPing(Server server, int repeat)
            {
                this.server = server;
                this.repeat = repeat;
                RoundtripTime = new List<int?>(repeat);
                failedpings = 0;
                pingTimes = repeat;
                ping = new Ping();
                ping.PingCompleted += Ping_PingCompleted;
            }

            public void Start()
            {
                if (server.server == "")
                {
                    FireCompleted(new Exception("Invalid Server"));
                    return;
                }
                new Task(() => ICMPTest(0)).Start();
            }

            private void ICMPTest(int delay)
            {
                try
                {
                    Logging.Debug($"Ping {server.server},times：{repeat}");
                    if (ip == null)
                    {
                        ip = Dns.GetHostAddresses(server.server)
                                .First(
                                    ip =>
                                        ip.AddressFamily == AddressFamily.InterNetwork ||
                                        ip.AddressFamily == AddressFamily.InterNetworkV6);
                    }
                    repeat--;
                    if (delay > 0)
                        Thread.Sleep(delay);
                    ping.SendAsync(ip, TimeoutMilliseconds);
                }
                catch (Exception e)
                {
                    //Logging.Error($"An exception occured while eveluating {server.FriendlyName()}");
                    Logging.LogUsefulException(e);
                    FireCompleted(e);
                }
            }

            private void Ping_PingCompleted(object sender, PingCompletedEventArgs e)
            {
                try
                {
                    if (e.Reply.Status == IPStatus.Success)
                    {
                        //Logging.Debug($"Ping {server.FriendlyName()} {e.Reply.RoundtripTime} ms");
                        RoundtripTime.Add((int?)e.Reply.RoundtripTime);
                    }
                    else
                    {
                        //Logging.Debug($"Ping {server.FriendlyName()} timeout");
                        failedpings += 1;
                        RoundtripTime.Add(0);
                    }
                    TestNext();
                }
                catch (Exception ex)
                {
                    //Logging.Error($"An exception occured while eveluating {server.FriendlyName()}");
                    Logging.LogUsefulException(ex);
                    FireCompleted(ex);
                }
            }

            private void TestNext()
            {
                if (repeat > 0)
                {
                    //Do ICMPTest in a random frequency
                    //int delay = new Random().Next() % TimeoutMilliseconds;
                    new Task(() => ICMPTest(0)).Start();
                }
                else
                {
                    FireCompleted(null);
                }
            }

            private void FireCompleted(Exception error)
            {
                Completed?.Invoke(this, new CompletedEventArgs
                {
                    Error = error,
                    Server = server,
                    RoundtripTime = RoundtripTime,
                    faildePings = failedpings,
                    pingTimes = pingTimes,
                    ip = (ip != null ? ip.ToString() : "null")
                });
            }

            public class CompletedEventArgs : EventArgs
            {
                public Exception Error;
                public Server Server;
                public List<int?> RoundtripTime;
                public int faildePings;
                public int pingTimes;
                public string ip;
            }
        }

    }
}
