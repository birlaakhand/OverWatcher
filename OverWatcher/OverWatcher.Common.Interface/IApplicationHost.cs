using System;
using System.Collections.Generic;
using System.Text;

namespace OverWatcher.Common.Interface
{
    public interface IApplicationHost
    {
        public static void Start(string[] args);

        public static void Stop();
    }
}
