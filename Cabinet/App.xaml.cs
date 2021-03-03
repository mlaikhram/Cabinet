using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Cabinet
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        [STAThread]
        public static void Main()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process existingProcess = Process.GetProcessesByName(currentProcess.ProcessName).Where(process => process.Id != currentProcess.Id).FirstOrDefault();
            if (existingProcess == null)
            {
                App application = new App();
                application.InitializeComponent();
                application.Run();
            }
            else
            {
                Console.WriteLine("process already running");
                System.Windows.Forms.SendKeys.SendWait("^%c");
            }
        }
    }
}
