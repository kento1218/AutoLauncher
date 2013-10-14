using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using AutoLauncherSrv;
using AutoLauncherSrv.RdpClient;

namespace TestApp
{
    class Program
    {
        static RdpClient client;

        static void Main(string[] args)
        {
            var uiThread = new Thread(UIThread);
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();

            while (true)
            {
                var line = Console.In.ReadLine();

                if (line == "quit")
                {
                    break;
                }
                if (line.StartsWith("connect "))
                {
                    var connArg = line.Split(new char[] { ' ' });
                    var host = connArg[1];
                    var user = connArg[2];
                    var pass = connArg[3];
                    Console.WriteLine("connect to {0}", host);
                    client.Connect(host, user, pass);
                    continue;
                }
                if (line.StartsWith("close"))
                {
                    client.Disconnect();
                    continue;
                }
                if (line.StartsWith("exec "))
                {
                    var connArg = line.Split(new char[] { ' ' });
                    var cmdline = connArg[1];
                    var user = connArg[2];
                    var pass = connArg[3];

                    var mng = new SessionManager();
                    mng.RdpClient = client;
                    mng.CurrentLogger = Console.WriteLine;
                    mng.CheckAndExec(null, cmdline, user, pass);
                    continue;
                }
                if (line.StartsWith("config"))
                {
                    var mng = new SessionManager();
                    mng.SaveSettings();
                }
                if (line.StartsWith("load"))
                {
                    var mng = new SessionManager();
                    mng.LoadSettings();
                }
            }

            Application.Exit();
            uiThread.Join();
        }

        protected static void UIThread()
        {
            client = new RdpClient();
            Application.Run();
        }
    }
}
