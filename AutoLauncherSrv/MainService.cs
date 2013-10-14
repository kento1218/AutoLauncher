using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using System.IO;

namespace AutoLauncherSrv
{
    public partial class MainService : ServiceBase
    {
        private SessionManager manager;

        public MainService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var uiThread = new Thread(UIThread);
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
        }

        protected override void OnStop()
        {
            manager.Stop();
            Application.Exit();
        }

        protected void UIThread()
        {
            var client = new RdpClient.RdpClient();

            manager = new SessionManager();
            manager.RdpClient = client;
            manager.CurrentLogger = EventLog.WriteEntry;
            manager.LoadSettings();
            manager.Start();

            Application.Run();
        }
    }
}
