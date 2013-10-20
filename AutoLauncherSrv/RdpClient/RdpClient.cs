using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MSTSCLib;
using System.Threading;

namespace AutoLauncherSrv.RdpClient
{
    public class RdpClient
    {
        private RdpClientForm form;

        public RdpClient()
        {
            form = new RdpClientForm();
        }

        public void Connect(string host, int port, string user, string password)
        {
            var client = form.RdpClient;

            form.BeginInvoke(new Action(
                () =>
                {
                    client.Server = host;
                    client.UserName = user;
                    client.AdvancedSettings2.ClearTextPassword = password;
                    client.AdvancedSettings5.RDPPort = port;

                    client.Connect();
                }));
        }

        public void Disconnect()
        {
            var client = form.RdpClient;

            form.BeginInvoke(new Action(
                () =>
                {
                    client.Disconnect();
                }));
        }
    }
}
