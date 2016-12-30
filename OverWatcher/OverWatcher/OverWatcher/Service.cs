﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher
{
    public class Service : ServiceBase
    {

        public Service()
        {
            ServiceName = Program.ServiceName;
        }
        protected override void OnStart(string[] args)
        {
            Program.Start(args);
        }
        protected override void OnStop()
        {
            Program.Stop();
        }

        private void InitializeComponent()
        {
            // 
            // Service
            // 
            this.AutoLog = false;
            this.CanShutdown = true;
            this.ServiceName = "TradeReconOverWatcher";

        }
    }
}
