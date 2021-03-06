﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.Common.Interface
{
    internal abstract class COMInterfaceBase : IDisposable
    {

        #region Clean Up
        protected enum COMCloseType { Exit, DecrementRefCount };
        private readonly List<Tuple<dynamic, Type>> COMList;
        protected List<Type> ClosableComList;
        protected abstract void CleanUpSetup();
        protected COMInterfaceBase()
        {
            COMList = new List<Tuple<dynamic, Type>>();
            ClosableComList = new List<Type>();
        }

        ~COMInterfaceBase()
        {
            Dispose(false);
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
                if (com != null)
                {
                    if (ClosableComList.Any(t => t == com.Item2))
                    {
                        com.Item1.Close();
                    }
                    re(com.Item1);
                }
            });

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        protected abstract void Dispose(bool disposing);
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
