using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using LibHotKeys;

namespace Cabinet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon = null;

        public MainWindow()
        {
            InitializeComponent();
            Console.WriteLine("started");

            // keyboard shortcut
            HotKeyController.Instance.RegisterHotKey("Open Cabinet", KeyModifiers.CONTROL | KeyModifiers.Alt | KeyModifiers.NOREPEAT, Keys.V, new Action<HotKey>(OpenWindow));

            // tray icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Click += new EventHandler(NotifyIcon_Click);
            //notifyIcon.DoubleClick += new EventHandler(notifyIcon_DoubleClick);
            string logo = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "logo.ico");
            notifyIcon.Icon = new System.Drawing.Icon(logo);
            notifyIcon.Visible = true;

            // TODO: listen for clipboard changes
        }


        void MainWindow_Deactivated(object sender, EventArgs e)
        {
            Console.WriteLine("deactivated");
            WindowState = WindowState.Minimized;
        }

        public void OpenWindow(HotKey k)
        {
            Console.WriteLine("opening from shortcut: " + k.Purpose);
            WindowState = WindowState.Normal;
            Activate();
        }

        void NotifyIcon_Click(object sender, EventArgs e)
        {
            Console.WriteLine("clicked toolbar icon");
        }

        private void test_button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("closing");
            WindowState = WindowState.Minimized;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("copying to clipboard content");
            System.Windows.Clipboard.SetText("looks like its working");
            WindowState = WindowState.Minimized;
        }
    }
}
