using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoLauncherSrv
{
    public class Config
    {
        public int rdpPort = 0;
        public int poolingInterval = 3;
        public int rdpWait = 5;

        public User[] users;
        public ExecInfo[] execInfo;
    }

    public class User
    {
        public string name;
        public string password;
    }

    public class ExecInfo
    {
        public string user;
        public string commandLine;
        public int? processID;
    }
}
