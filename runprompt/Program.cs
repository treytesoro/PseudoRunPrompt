using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace runprompt
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var sessionid = System.Diagnostics.Process.GetCurrentProcess().SessionId;
            using (Mutex mutex = new Mutex(false, "Global\\" + sessionid))
            {
                if (!mutex.WaitOne(0, false))
                {
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }

        }
    }
}
