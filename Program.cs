using System;
using System.Linq;
using System.Windows.Forms;

namespace NetworkMonitor
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool isAutoStartLaunch = args.Any(arg => string.Equals(arg, StartupServiceManager.AutoStartArgument, StringComparison.OrdinalIgnoreCase));
            Application.Run(new MainForm(isAutoStartLaunch));
        }
    }
}
