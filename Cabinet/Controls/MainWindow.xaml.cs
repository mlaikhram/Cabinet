using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
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
            Directory.CreateDirectory(Paths.LOCAL_STORAGE_DIRECTORY);
            CategoryForm.ParentWindow = this;
            ClipboardForm.ParentWindow = this;
            //recentClipboardObjects = new LinkedList<ClipboardObject>();
            categories = new List<Category>();
            Category recentCategory = new Category(this);
            AddCategory(recentCategory, true);
            DBManager.Instance.GetCategories(this).ForEach((category) => AddCategory(category));

            AddCategoryBorder.MouseEnter += (sender, e) => ((System.Windows.Controls.Image) AddCategoryBorder.Child).Margin = new Thickness(5);
            AddCategoryBorder.MouseLeave += (sender, e) => ((System.Windows.Controls.Image) AddCategoryBorder.Child).Margin = new Thickness(10);

            selfCopy = false;

            // tray icon
            notifyIcon = new NotifyIcon();
            //notifyIcon.DoubleClick += new EventHandler(notifyIcon_DoubleClick);
            notifyIcon.Icon = Images.LOGO;
            notifyIcon.Visible = true;
            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add("Open", null, (sender, e) => {
                Console.WriteLine("clicked open in context menu");
                OpenWindow(null);
            });
            contextMenuStrip.Items.Add("Quit", null, (sender, e) => {
                Console.WriteLine("clicked quit in context menu");
                System.Environment.Exit(0);
            });
            notifyIcon.ContextMenuStrip = contextMenuStrip;

            // keyboard shortcut
            HotKeyController.Instance.RegisterHotKey("Open Cabinet", KeyModifiers.CONTROL | KeyModifiers.Alt | KeyModifiers.NOREPEAT, Keys.V, new Action<HotKey>(OpenWindow));

            // listen for clipboard changes
            ClipboardEventController.Instance.RegisterClipboardEvent("Add to Recents", new Action(SaveClipboardToRecent));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // prevent screen capture
            DisplayController.Instance.DisableDisplayCapture(new WindowInteropHelper(this).Handle);
        }

        public void OpenWindow(HotKey k)
        {
            WindowState = WindowState.Normal;

            // position window at cursor, but ensure full visibility
            int buffer = 10;
            Rectangle screenBounds = Screen.FromPoint(System.Windows.Forms.Control.MousePosition).Bounds;

            Left = Math.Min(System.Windows.Forms.Control.MousePosition.X + buffer, screenBounds.X + screenBounds.Width - Width - buffer);
            Top = Math.Max(System.Windows.Forms.Control.MousePosition.Y - Height - buffer, screenBounds.Y + buffer);

            Show();
            Activate();
        }

        public void AddCategory(Category category, bool setActive = false)
        {
            CategoryPanel.Children.Insert(CategoryPanel.Children.Count - 1, category.Icon);
            categories.Add(category);
            if (setActive)
            {
                SetActiveCategory(category.Id);
            }
        }

        public void MoveCategory(Category target, Category destination)
        {
            int targetIndex = CategoryPanel.Children.IndexOf(target.Icon);
            int destinationIndex = CategoryPanel.Children.IndexOf(destination.Icon);

            if (targetIndex != destinationIndex)
            {
                CategoryPanel.Children.RemoveAt(targetIndex);
                categories.RemoveAt(targetIndex);
                CategoryPanel.Children.Insert(destinationIndex, target.Icon);
                categories.Insert(destinationIndex, target);
                // TODO: db update for reorder
                Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() =>
                {
                    for (int i = Math.Min(targetIndex, destinationIndex); i < categories.Count; ++i)
                    {
                        DBManager.Instance.UpdateCategoryOrder(categories[i].Id, i);
                    }
                }));
            }
        }

        public void SetActiveCategory(long id)
        {
            Category selectedCategory = categories.FirstOrDefault((category) => category.Id == id);
            if (selectedCategory != null && selectedCategory.Id != CurrentCategoryId)
            {
                CurrentCategory.Content = selectedCategory.Name;
                CurrentCategoryId = selectedCategory.Id;
                UpdateContentPanel(selectedCategory, Search.Text);
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
                    categories[0].RemoveClipboardObject(duplicateObject);
                    if (CurrentCategoryId == categories[0].Id)
                    {
                        ContentPanel.Children.Remove(duplicateObject.ClipboardContainer);
                    }
                    duplicateObject.Name = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    categories[0].AddClipboardObject(duplicateObject);
                    if (CurrentCategoryId == categories[0].Id)
                    {
                        ContentPanel.Children.Insert(1, duplicateObject.ClipboardContainer);
                    }
                }
                else
                {
                    ClipboardObject newClipboardObject = null;
                    Console.WriteLine("not a duplicate, finding format to save as");
                    foreach (string format in Clipboard.GetDataObject().GetFormats()) Console.WriteLine(format);
                    if (Clipboard.ContainsText())
                    {
                        newClipboardObject = new TextClipboardObject(this, DateTime.Now.ToString(Recent.DATE_FORMAT), Clipboard.GetText());
                    }
                    else if (Clipboard.ContainsImage())
                    {
                        newClipboardObject = new ImageClipboardObject(this, DateTime.Now.ToString(Recent.DATE_FORMAT), Clipboard.GetDataObject());
                    }
                    else if (Clipboard.ContainsFileDropList())
                    {
                        newClipboardObject = new FileDropListClipboardObject(this, DateTime.Now.ToString(Recent.DATE_FORMAT), Clipboard.GetFileDropList());
                    }
                    if (newClipboardObject != null)
                    {
                        categories[0].AddClipboardObject(newClipboardObject);
                        if (CurrentCategoryId == categories[0].Id)
                        {
                            ContentPanel.Children.Insert(1, newClipboardObject.ClipboardContainer);
                        }
                    }
                }
                SetActiveCategory(Recent.ID);
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
            Hide();
            WindowState = WindowState.Minimized;
        }

        void MainWindow_Deactivated(object sender, EventArgs e)
        {
            HideWindow();
        }

        //void NotifyIcon_Click(object sender, EventArgs e)
        //{
        //    Console.WriteLine("clicked toolbar icon");
        //    OpenWindow(null);
        //}

        private void Window_Closed(object sender, EventArgs e)
        {
            // keyboard shortcut
            HotKeyController.Instance.UnRegisterHotKey("Open Cabinet");

            // clipboard event
            ClipboardEventController.Instance.UnRegisterClipboardEvent("Add to Recents");
        }

        public void ClearRecents()
        {
            Category recents = categories.FirstOrDefault((category) => category.Id == Recent.ID);
            if (recents != null) {
                recents.ClearClipboardObjects();
                if (CurrentCategoryId == Recent.ID)
                {
                    UpdateContentPanel(recents, Search.Text);
                }
            }
        }

        private void CreateCategory(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Console.WriteLine("creatiing category form");
            CategoryForm.OpenCreateForm();
        }

        public void DeleteCategory(long id)
        {
            Category categoryToDelete = categories.FirstOrDefault((category) => category.Id == id);
            if (categoryToDelete != null)
            {
                CategoryPanel.Children.Remove(categoryToDelete.Icon);
                categories.Remove(categoryToDelete);
                if (CurrentCategoryId == id)
                {
                    SetActiveCategory(Recent.ID);
                }
                Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() =>
                {
                    Console.WriteLine("deleting category internally");
                    HashSet<string> internalContent = new HashSet<string>();
                    foreach (ClipboardObject clipboardObject in categoryToDelete.ClipboardObjects)
                    {
                        if (clipboardObject.UsesInternalStorage)
                        {
                            internalContent.Add(clipboardObject.GenerateContentString());
                        }
                        DBManager.Instance.DeleteClipboardObject(clipboardObject.Id);
                    }
                    string[] internalContentArr = new string[internalContent.Count];
                    internalContent.CopyTo(internalContentArr);
                    foreach (string unusedFile in DBManager.Instance.FindUnusedStorageFiles(internalContentArr))
                    {
                        Console.WriteLine("removing unused file: " + unusedFile);
                        try
                        {
                            File.Delete(unusedFile);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                    DBManager.Instance.DeleteCategory(id);
                    Console.WriteLine("category deleted");
                }));
            }
        }

        public void DeleteClipboardObject(long id)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() =>
            {
                Category currentCategory = categories.FirstOrDefault((category) => category.Id == CurrentCategoryId);
                if (currentCategory != null)
                {
                    ClipboardObject objectToRemove = currentCategory.ClipboardObjects.FirstOrDefault((clipboardObject) => clipboardObject.Id == id);
                    if (objectToRemove != null)
                    {
                        if (CurrentCategoryId != Recent.ID)
                        {
                            DBManager.Instance.DeleteClipboardObject(id);
                            if (objectToRemove.UsesInternalStorage)
                            {
                                foreach (string unusedFile in DBManager.Instance.FindUnusedStorageFiles(objectToRemove.GenerateContentString()))
                                {
                                    Console.WriteLine("removing unused file: " + unusedFile);
                                    try
                                    {
                                        File.Delete(unusedFile);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                    }
                                }
                            }
                            // TODO: cleanup local files if necessary
                        }
                        ContentPanel.Children.Remove(objectToRemove.ClipboardContainer);
                        currentCategory.RemoveClipboardObject(objectToRemove);
                    }
                }
            }));
        }

        public Category GetCurrentCategory()
        {
            return categories.FirstOrDefault((category) => category.Id == CurrentCategoryId);
        }

        public void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            Console.WriteLine("Performing Search: {0}", Search.Text);
            Category currentCategory = categories.Find((category) => category.Id == CurrentCategoryId);
            if (currentCategory != null)
            {
                UpdateContentPanel(currentCategory, Search.Text);
            }
        }

        public void UpdateContentPanel(Category category, string filter)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() =>
            {
                ContentPanel.Children.RemoveRange(1, ContentPanel.Children.Count - 1);
                foreach (ClipboardObject clipboardObject in category.ClipboardObjects
                .Where((clipboardObject) => filter.Trim() == "" || clipboardObject.MatchesSearch(filter.Trim().ToLower())).ToList())
                {
                    ContentPanel.Children.Add(clipboardObject.ClipboardContainer);
                }
            }));
        }
    }
}
