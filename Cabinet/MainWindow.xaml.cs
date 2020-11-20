using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WinApiWrappers;

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

            // tray icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Click += new EventHandler(NotifyIcon_Click);
            //notifyIcon.DoubleClick += new EventHandler(notifyIcon_DoubleClick);
            string logo = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "logo.ico");
            notifyIcon.Icon = new Icon(logo);
            notifyIcon.Visible = true;

            // keyboard shortcut
            HotKeyController.Instance.RegisterHotKey("Open Cabinet", KeyModifiers.CONTROL | KeyModifiers.Alt | KeyModifiers.NOREPEAT, Keys.V, new Action<HotKey>(OpenWindow));

            // listen for clipboard changes
            ClipboardEventController.Instance.RegisterClipboardEvent("Add to Recents", new Action(SaveClipboardToRecent));
        }

        public void OpenWindow(HotKey k)
        {
            WindowState = WindowState.Normal;

            // position window at cursor, but ensure full visibility
            int buffer = 10;
            Rectangle screenBounds = Screen.FromPoint(Control.MousePosition).Bounds;

            Left = Math.Min(Control.MousePosition.X + buffer, screenBounds.X + screenBounds.Width - Width - buffer);
            Top = Math.Max(Control.MousePosition.Y - Height - buffer, screenBounds.Y + buffer);

            Show();
            Activate();
        }

        public void SaveClipboardToRecent()
        {
            Console.WriteLine("saving clipboard");

            // TODO: save to list of recent clipboard objects to display in main window
        }

        void MainWindow_Deactivated(object sender, EventArgs e)
        {
            Console.WriteLine("deactivated");
            Hide();
            WindowState = WindowState.Minimized;
        }

        void NotifyIcon_Click(object sender, EventArgs e)
        {
            Console.WriteLine("clicked toolbar icon");
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("copying to clipboard content");
            System.Windows.Clipboard.SetText("looks like its working");
            WindowState = WindowState.Minimized;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // keyboard shortcut
            HotKeyController.Instance.UnRegisterHotKey("Open Cabinet");

            // clipboard event
            ClipboardEventController.Instance.UnRegisterClipboardEvent("Add to Recents");
        }
    }
}
