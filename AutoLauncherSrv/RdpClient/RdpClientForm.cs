using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AutoLauncherSrv.RdpClient
{
    public partial class RdpClientForm : Form
    {
        public RdpClientForm()
        {
            InitializeComponent();
        }

        public AxMSTSCLib.AxMsRdpClient5NotSafeForScripting RdpClient { get { return rdpClient; } }
    }
}
