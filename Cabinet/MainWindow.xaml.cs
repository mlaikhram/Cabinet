using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WinApiWrappers;
using Clipboard = System.Windows.Forms.Clipboard;

namespace Cabinet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon = null;
        private LinkedList<ClipboardObject> recentClipboardObjects;
        private bool selfCopy;

        public MainWindow()
        {
            InitializeComponent();
            recentClipboardObjects = new LinkedList<ClipboardObject>();
            selfCopy = false;

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
            Console.WriteLine("attempting to save clipboard to recent");
            if (!selfCopy)
            {
                Console.WriteLine("checking for recent duplicate");
                ClipboardObject duplicateObject = null;
                foreach (ClipboardObject clipboardObject in recentClipboardObjects)
                {
                    if (clipboardObject.MatchesClipboard())
                    {
                        duplicateObject = clipboardObject;
                        break;
                    }
                }
                if (duplicateObject != null)
                {
                    Console.WriteLine("moving duplicate clipboard to most recent");
                    recentClipboardObjects.Remove(duplicateObject);
                    ContentPanel.Children.Remove(duplicateObject.ClipboardContainer);
                    duplicateObject.Label = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    recentClipboardObjects.AddFirst(duplicateObject);
                    ContentPanel.Children.Insert(0, duplicateObject.ClipboardContainer);
                }
                else
                {
                    Console.WriteLine("not a duplicate, finding format to save as");
                    if (Clipboard.ContainsText())
                    {
                        Console.WriteLine(Clipboard.GetText());

                        TextClipboardObject textClipboardObject = new TextClipboardObject(this, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), Clipboard.GetText());
                        recentClipboardObjects.AddFirst(textClipboardObject);
                        ContentPanel.Children.Insert(0, textClipboardObject.ClipboardContainer);
                    }
                    else if (Clipboard.ContainsImage())
                    {
                        ImageClipboardObject imageClipboardObject = new ImageClipboardObject(this, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), Clipboard.GetImage());
                        recentClipboardObjects.AddFirst(imageClipboardObject);
                        ContentPanel.Children.Insert(0, imageClipboardObject.ClipboardContainer);
                    }
                    else if (Clipboard.ContainsFileDropList())
                    {
                        FileDropListClipboardObject fileDropListClipboardObject = new FileDropListClipboardObject(this, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), Clipboard.GetFileDropList());
                        recentClipboardObjects.AddFirst(fileDropListClipboardObject);
                        ContentPanel.Children.Insert(0, fileDropListClipboardObject.ClipboardContainer);
                    }
                    // TODO: save to list of recent clipboard objects to display in main window
                    // wrap panel for clipboard list
                }
            }
            else
            {
                Console.WriteLine("ignoring self copy");
                selfCopy = false;
            }
        }

        public void IncomingSelfCopy()
        {
            selfCopy = true;
        }

        public void HideWindow()
        {
            Console.WriteLine("deactivated");
            Hide();
            WindowState = WindowState.Minimized;
        }

        void MainWindow_Deactivated(object sender, EventArgs e)
        {
            HideWindow();
        }

        void NotifyIcon_Click(object sender, EventArgs e)
        {
            Console.WriteLine("clicked toolbar icon");
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
