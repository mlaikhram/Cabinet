using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
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
        //private LinkedList<ClipboardObject> recentClipboardObjects;
        private readonly List<Category> categories;
        public ReadOnlyCollection<Category> Categories => categories.AsReadOnly();
        public long CurrentCategoryId { get; private set; }
        private bool selfCopy;

        public MainWindow()
        {
            InitializeComponent();
            CategoryForm.ParentWindow = this;
            //recentClipboardObjects = new LinkedList<ClipboardObject>();
            categories = new List<Category>();
            Category recentCategory = new Category(this);
            AddCategory(recentCategory);
            // TODO: loop over db entries to add categories
            SetActiveCategory(recentCategory.Id);
            // DBManager.Instance.AddCategory("test", "icons/default.png", Colors.Red);

            AddCategoryBorder.MouseEnter += (sender, e) => ((System.Windows.Controls.Image) AddCategoryBorder.Child).Margin = new Thickness(5);
            AddCategoryBorder.MouseLeave += (sender, e) => ((System.Windows.Controls.Image) AddCategoryBorder.Child).Margin = new Thickness(10);

            selfCopy = false;

            // tray icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Click += new EventHandler(NotifyIcon_Click);
            //notifyIcon.DoubleClick += new EventHandler(notifyIcon_DoubleClick);
            notifyIcon.Icon = new Icon(Paths.LOGO);
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

        public void AddCategory(Category category)
        {
            CategoryPanel.Children.Insert(CategoryPanel.Children.Count - 1, category.Icon);
            categories.Add(category);
            SetActiveCategory(category.Id);
        }

        public void SetActiveCategory(long id)
        {
            Category selectedCategory = categories.Where((category) => category.Id == id).FirstOrDefault();
            if (selectedCategory != null && selectedCategory.Id != CurrentCategoryId)
            {
                CurrentCategory.Content = selectedCategory.Name;
                ContentPanel.Children.RemoveRange(1, ContentPanel.Children.Count - 1);
                Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() =>
                {
                    foreach (ClipboardObject clipboardObject in selectedCategory.ClipboardObjects)
                    {
                        ContentPanel.Children.Add(clipboardObject.ClipboardContainer);
                    }
                }));
                CurrentCategoryId = selectedCategory.Id;
            }
        }

        public void SaveClipboardToRecent()
        {
            Console.WriteLine("attempting to save clipboard to recent");
            if (!selfCopy)
            {
                Console.WriteLine("checking for recent duplicate");
                ClipboardObject duplicateObject = null;
                foreach (ClipboardObject clipboardObject in categories[0].ClipboardObjects)
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
                    categories[0].RemoveClipboardObject(duplicateObject, false);
                    ContentPanel.Children.Remove(duplicateObject.ClipboardContainer);
                    duplicateObject.Label = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    categories[0].AddClipboardObject(duplicateObject, false);
                    ContentPanel.Children.Insert(1, duplicateObject.ClipboardContainer);
                }
                else
                {
                    ClipboardObject newClipboardObject = null;
                    Console.WriteLine("not a duplicate, finding format to save as");
                    if (Clipboard.ContainsText())
                    {
                        newClipboardObject = new TextClipboardObject(this, DateTime.Now.ToString(Recent.DATE_FORMAT), Clipboard.GetText());
                    }
                    else if (Clipboard.ContainsImage())
                    {
                        newClipboardObject = new ImageClipboardObject(this, DateTime.Now.ToString(Recent.DATE_FORMAT), Clipboard.GetImage());
                    }
                    else if (Clipboard.ContainsFileDropList())
                    {
                        newClipboardObject = new FileDropListClipboardObject(this, DateTime.Now.ToString(Recent.DATE_FORMAT), Clipboard.GetFileDropList());
                    }
                    if (newClipboardObject != null)
                    {
                        categories[0].AddClipboardObject(newClipboardObject, false);
                        if (CurrentCategoryId == categories[0].Id)
                        {
                            ContentPanel.Children.Insert(1, newClipboardObject.ClipboardContainer);
                        }
                    }
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

        private void CreateCategory(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Console.WriteLine("creatiing category form");
            CategoryForm.CreateCategoryForm();
        }
    }
}
