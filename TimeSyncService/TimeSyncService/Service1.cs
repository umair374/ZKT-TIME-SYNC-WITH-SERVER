using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using Oracle.ManagedDataAccess.Client;
using TimeSyncService.Utilities;

namespace TimeSyncService
{
    public partial class Service1 : ServiceBase
    {
        private ZkemClient objZkeeper;
        public Service1()
        {
            InitializeComponent();
            Action<object, string> raiseEvent = (sender, message) =>
            {
                // Handle the event, log the message, etc.
                Console.WriteLine($"Event raised: {message}");
            };

            objZkeeper = new ZkemClient(raiseEvent);

        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is started at " + DateTime.Now);
            SyncAllDevices(null, null);
            InitializeSyncTimer();
        }

        protected override void OnStop()
        {
            syncTimer.Stop();
        }

        private Timer syncTimer;

        private void InitializeSyncTimer()
        {
            syncTimer = new Timer();
            syncTimer.Interval = 2 * 60 * 60 * 1000; // Every 2 hours
            //syncTimer.Interval = 10 * 60 * 1000; // Every 10 minute
            syncTimer.Elapsed += SyncAllDevices;
            syncTimer.Start();
        }

        private void SyncAllDevices(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.DayOfWeek != DayOfWeek.Monday)
            {
                WriteToFile("Skipped sync - Today is not Monday.");
                WriteToFile(" ");
                WriteToFile("****************************************************************************");
                WriteToFile(" ");
                return;
            }
            WriteToFile("Sync started at " + DateTime.Now);
            DateTime serverTime = GetServerTime();

            if (serverTime == DateTime.MinValue)
            {
                WriteToFile("Failed to retrieve server time.");
                return;
            }

            var devices = GetDevices();

            foreach (var device in devices)
            {
                //WriteToFile($"Attempting to connect to device IP: {device.IP}");
                bool isConnected = objZkeeper.Connect_Net(device.IP, device.Port);

                if (isConnected)
                {
                    WriteToFile("Connected Device IP : " + device.IP);
                    objZkeeper.SetDeviceTime(device.DeviceId, serverTime);
                    //if (objZkeeper.SetDeviceTime2(device.DeviceId, serverTime.Year, serverTime.Month, serverTime.Day, serverTime.Hour, serverTime.Minute, serverTime.Second))
                    //{
                    //    WriteToFile("Date Updated : " + serverTime);
                    //}
                    //else
                    //{
                    //    WriteToFile("Failed to update date on device IP: " + device.IP);
                    //}
                    objZkeeper.Disconnect();
                }
                else
                {
                    WriteToFile("Connection Failed With Device IP : " + device.IP);
                }
            }

            WriteToFile("Sync Done at " + DateTime.Now);
            WriteToFile(" " );
            WriteToFile("****************************************************************************");
            WriteToFile(" ");
            WriteToFile(" ");

        }

        private DateTime GetServerTime()
        {
            string oradb = "User Id=SOLEHRE;Password=SOLEHRESOLAPPS;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.236)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)))";
            DateTime serverTime = DateTime.MinValue;

            using (OracleConnection connection = new OracleConnection(oradb))
            {
                try
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand("SELECT SYSDATE FROM DUAL", connection))
                    {
                        object result = command.ExecuteScalar();
                        if (result != null && DateTime.TryParse(result.ToString(), out serverTime))
                        {
                            // Log successful time retrieval
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error
                }
            }

            return serverTime;
        }

        public void WriteToFile(string Message)
        {
    
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string filepath = Path.Combine(path, "ServiceLog_" + DateTime.Now.ToString("yyyyMMdd") + ".log");

                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine($"{DateTime.Now}: {Message}");
                }
            }

        }
        private List<DeviceInfo> GetDevices()
                {
                    return new List<DeviceInfo>
                            {
                                new DeviceInfo { IP = "192.168.1.201", Port = 4370, DeviceId = 1 },
                                new DeviceInfo { IP = "192.168.1.52", Port = 4370, DeviceId = 1 },
                                new DeviceInfo { IP = "192.168.1.200", Port = 4370, DeviceId = 1 },
                                new DeviceInfo { IP = "192.168.1.207", Port = 4370, DeviceId = 1 },
                                new DeviceInfo { IP = "192.168.1.205", Port = 4370, DeviceId = 1 },
                                new DeviceInfo { IP = "192.168.1.203", Port = 4370, DeviceId = 1 },
                                new DeviceInfo { IP = "192.168.1.202", Port = 4370, DeviceId = 1 },
                                // Add more devices as needed
                            };
                }
    }
}
