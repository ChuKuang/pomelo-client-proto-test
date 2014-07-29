using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pomelo_NativeSocket {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += new ThreadExceptionEventHandler(ApplicationThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomainUnhandledException);

            Application.Run(new Main());
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //MessageBox.Show("UnhandledException" + e.ToString());
            System.Console.WriteLine("UnhandledException" + e.ToString());
        }

        private static void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            //MessageBox.Show("ThreadException" + e.ToString());
            System.Console.WriteLine("ThreadException" + e.ToString());
        }
    }
}
