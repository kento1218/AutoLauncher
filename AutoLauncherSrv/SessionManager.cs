using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoLauncherSrv.RdpClient;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using Newtonsoft.Json;

namespace AutoLauncherSrv
{
    public class SessionManager
    {
        public RdpClient.RdpClient RdpClient { get; set; }
        public Logger CurrentLogger { get; set; }

        public delegate void Logger(string message);

        private Config config;

        public SessionManager()
        {
            CurrentLogger = NullLogger;
            config = new Config() { users = new User[0], execInfo = new ExecInfo[0] };
        }

        private void NullLogger(string message)
        {
        }

        protected string GetConfigFilePath()
        {
            var execpath = Assembly.GetEntryAssembly().Location;
            return new FileInfo(execpath).DirectoryName + "\\config.txt";
        }

        public void LoadSettings()
        {
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(GetConfigFilePath()))
            using (var json = new JsonTextReader(sr))
            {
                config = serializer.Deserialize<Config>(json);
            }
        }

        public void SaveSettings()
        {
            var serializer = new JsonSerializer();
            using (var sw = new StreamWriter(GetConfigFilePath()))
            using (var json = new JsonTextWriter(sw))
            {
                json.Formatting = Formatting.Indented;
                serializer.Serialize(json, config);
            }
        }

        private bool stop;
        private Thread thread;

        private void ThreadMain()
        {
            stop = false;
            CurrentLogger("Thread started");
            while (!stop)
            {
                Thread.Sleep(50);
                ExecMonitor();
            }
            CurrentLogger("Thread stoped");
        }

        public void Start()
        {
            thread = new Thread(ThreadMain);
            thread.Start();
        }

        public void Stop()
        {
            stop = true;
            thread.Join();
        }

        public void ExecMonitor()
        {
            Dictionary<string, User> userDict = new Dictionary<string, User>();
            foreach (var user in config.users)
            {
                userDict[user.name] = user;
            }

            foreach (var execinfo in config.execInfo)
            {
                var user = userDict[execinfo.user];

                int? processID = execinfo.processID;
                CheckAndExec(ref processID, execinfo.commandLine, user.name, user.password);
                if (processID != execinfo.processID)
                {
                    execinfo.processID = processID;
                    SaveSettings();
                }

                Thread.Sleep(5);
            }
        }

        public void CheckAndExec(ref int? processID, string cmdline, string username, string password)
        {
            bool exists;
            if (processID == null)
            {
                exists = false;
            }
            else
            {
                try
                {
                    Process.GetProcessById(processID.Value);
                    exists = true;
                }
                catch (ArgumentException)
                {
                    exists = false;
                }
            }
            //CurrentLogger(string.Format("Process ({0}): {1}", processID, exists));

            if (!exists)
            {
                UInt32? sessionID = Win32API.GetSessionID(username);
                CurrentLogger(string.Format("Session: {0}", sessionID));
                if (sessionID == null)
                {
                    RdpClient.Connect("localhost", username, password);
                    while (sessionID == null)
                    {
                        Thread.Sleep(100);
                        sessionID = Win32API.GetSessionID(username);
                    }
                    Thread.Sleep(5000);
                    RdpClient.Disconnect();
                    CurrentLogger(string.Format("Created session: {0}", sessionID));
                }
                Win32API.CreateProcess(sessionID.Value, cmdline, out processID);
                CurrentLogger(string.Format("Created process: {0}", processID));
            }
        }
    }
}
