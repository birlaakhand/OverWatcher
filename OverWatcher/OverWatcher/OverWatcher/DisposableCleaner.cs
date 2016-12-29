using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher
{
    public class DisposableCleaner
    {
        private static List<IDisposable> DisposableResourceList = new List<IDisposable>();
        /// <summary>
        /// Manage any disposable COM and DB connection, must clean if program crashes
        /// </summary>
        /// <param name="d"></param>
        public static void ManageDisposable(IDisposable d)
        {
            DisposableResourceList.Add(d);
        }
        public static void Clean()
        {
            DisposableResourceList.ForEach(d => d?.Dispose());
        }

        public static void OnClose(object sender, EventArgs e)
        {
            Clean();
        }
    }
}
