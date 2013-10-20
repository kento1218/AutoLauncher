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
            try
            {
                using (var sr = new StreamReader(GetConfigFilePath()))
                using (var json = new JsonTextReader(sr))
                {
                    config = serializer.Deserialize<Config>(json);
                }
            }
            catch(FileNotFoundException)
            {
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
                Thread.Sleep(config.poolingInterval * 1000);
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

                var proc = CheckAndExec(execinfo.processID, execinfo.commandLine, user.name, user.password);
                if (proc != null && proc.Id != execinfo.processID)
                {
                    execinfo.processID = proc.Id;
                    SaveSettings();
                }

                Thread.Sleep(5);
            }
        }

        public Process CheckAndExec(int? processID, string cmdline, string username, string password)
        {
            if (processID != null)
            {
                try
                {
                    return Process.GetProcessById(processID.Value);
                }
                catch (ArgumentException)
                {
                }
            }

            UInt32? sessionID = Win32API.GetSessionID(username);
            CurrentLogger(string.Format("Session: {0}", sessionID));
            if (sessionID == null)
            {
                RdpClient.Connect("localhost", config.rdpPort, username, password);
                while (sessionID == null)
                {
                    Thread.Sleep(100);
                    sessionID = Win32API.GetSessionID(username);
                }
                Thread.Sleep(config.rdpWait * 1000);
                RdpClient.Disconnect();
                CurrentLogger(string.Format("Created session: {0}", sessionID));
            }

            if (!Win32API.CreateProcess(sessionID.Value, cmdline, out processID))
            {
                CurrentLogger(string.Format("Create Process Error: {0}", Marshal.GetLastWin32Error()));
                return null;
            }
            CurrentLogger(string.Format("Created process: {0}", processID));

            try
            {
                return Process.GetProcessById(processID.Value);
            }
            catch (ArgumentException)
            {
                CurrentLogger("Cannot Create Process");
                return null;
            }
        }
    }
}
