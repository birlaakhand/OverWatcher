using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.TradeReconMonitor.Core
{
    internal abstract class COMInterfaceBase
    {

        #region Clean Up
        protected enum COMCloseType { Exit, DecrementRefCount };
        private List<Tuple<dynamic, Type>> COMList;
        protected List<Type> closableCOMList;
        protected abstract void CleanUpSetup();
        protected COMInterfaceBase()
        {
            COMList = new List<Tuple<dynamic, Type>>();
            closableCOMList = new List<Type>();
        }
        protected virtual T GetCOM<T>(T com)
        {
            COMList.Add(new Tuple<dynamic, Type>(com, typeof(T)));
            return com;
        }
        delegate int ReleaseCOMFunc(object o);
        protected virtual void CloseCOM(COMCloseType type)
        {
            ReleaseCOMFunc re;
            if (type == COMCloseType.Exit)
            {
                re = Marshal.FinalReleaseComObject;
            }
            else
            {
                re = Marshal.ReleaseComObject;
            }
            COMList.ForEach(com =>
            {
                if (closableCOMList.Any(t => t == com.Item2))
                {
                    com.Item1.Close();
                }
                re(com.Item1);
            });

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        #endregion
    }
}
