using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.Common.CefSharpBase
{
    class CefSharpException : Exception
    {
        public CefSharpException(string msg) : base(msg) { }
    }
}
