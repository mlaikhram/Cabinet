using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Clipboard = System.Windows.Forms.Clipboard;
using DataObject = System.Windows.DataObject;
using IDataObject = System.Windows.Forms.IDataObject;
using FileAttributes = System.IO.FileAttributes;
using Image = System.Windows.Controls.Image;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace Cabinet
{
    public abstract class ClipboardObject : IComparable
    {
        private static bool IS_DRAGGING = false;
        private static long NEXT_RECENT_ID = 0;

        public long Id { get; private set; }

        private bool LoadedPreview { get; set; }
        private MainWindow parentWindow;
        private Label label;
        public string Name
        {
            get
            {
                return label.Content.ToString();
            }
            set
            {
                label.Content = value;
            }
        }

        private StackPanel StackPanel { get; set; }

        private Border clipboardContainer;
        public Border ClipboardContainer {
            get
            {
                if (!LoadedPreview)
                {
                    LoadedPreview = true;
                    Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() =>
                    {
                        AddClipboardPreviewPanelToStackPanel();
                        clipboardContainer.Child = StackPanel;
                    }));
                }
                return clipboardContainer;
            }
            private set
            {
                clipboardContainer = value;
            }
        }

        public bool UsesInternalStorage { get; protected set; }

        protected ClipboardObject(MainWindow parentWindow, string label) : this(parentWindow, NEXT_RECENT_ID++, label, false)
        {
        }

        protected ClipboardObject(MainWindow parentWindow, long id, string label, bool canEdit = true)
        {
            Id = id;
            LoadedPreview = false;
            UsesInternalStorage = false;
            this.parentWindow = parentWindow;

            StackPanel stackPanel;
            clipboardContainer = ControlUtils.CreateClipboardObjectContainer(label, new Thickness(6, 0, 0, 6), null, out this.label, out stackPanel);
            StackPanel = stackPanel;

            MenuItem updateItem = ControlUtils.CreateMenuItem("Edit");
            updateItem.IsEnabled = canEdit;
            if (canEdit)
            {
                updateItem.Click += (sender, e) => parentWindow.ClipboardForm.OpenUpdateForm(parentWindow.GetCurrentCategory(), this);
            }

            MenuItem deleteItem = ControlUtils.CreateMenuItem("Delete");
            deleteItem.Click += (sender, e) => parentWindow.ConfirmationForm.OpenForm(
                "Confirm Delete",
                () => parentWindow.DeleteClipboardObject(Id),
                ControlUtils.CreateClipboardObjectContainer(Name, new Thickness(20), () => GenerateClipboardPreviewPanel(), out _, out _),
                ControlUtils.CreateConfirmationText(string.Format("Are you sure you want to delete {0}?", Name))
            );

            clipboardContainer.ContextMenu = ControlUtils.CreateContextMenu();
            clipboardContainer.ContextMenu.Items.Add(updateItem);
            clipboardContainer.ContextMenu.Items.Add(deleteItem);

            clipboardContainer.MouseLeftButtonUp += TriggerClipboardCopy;
            clipboardContainer.MouseEnter += OnHoverEnter;
            clipboardContainer.MouseLeave += OnHoverExit;
            clipboardContainer.MouseMove += OnMouseDrag;
        }

        private void AddClipboardPreviewPanelToStackPanel()
        {
            FrameworkElement clipboardPreviewPanel = GenerateClipboardPreviewPanel();
            clipboardPreviewPanel.Height = 150;
            StackPanel.Children.Insert(0, clipboardPreviewPanel);
        }

        private void TriggerClipboardCopy(object sender, EventArgs e)
        {
            if (!IS_DRAGGING)
            {
                Console.WriteLine("triggering copy");
                parentWindow.IncomingSelfCopy();
                CopyContentToClipboard();
                parentWindow.HideWindow();
            }
        }

        private void OnMouseDrag(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !IS_DRAGGING)
            {
                IS_DRAGGING = true;
                Console.WriteLine("dragging");
                DataObject dataObject = GetDataObject();
                dataObject.SetData("ClipboardObject", this);
                DragDrop.DoDragDrop((Border)sender, dataObject, DragDropEffects.Copy | DragDropEffects.Move);
                Console.WriteLine("finished");
                IS_DRAGGING = false;
            }
            // TODO: visual drag effect
        }

        protected virtual void OnHoverEnter(object sender, EventArgs e)
        {
            ClipboardContainer.BorderBrush = new SolidColorBrush(Colors.White);
        }

        protected virtual void OnHoverExit(object sender, EventArgs e)
        {
            ClipboardContainer.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.CLIPBOARD_BORDER);
        }

        public abstract bool MatchesClipboard();
        protected abstract void CopyContentToClipboard();
        public abstract FrameworkElement GenerateClipboardPreviewPanel();
        public abstract string GenerateContentString();
        protected abstract DataObject GetDataObject();
        public abstract bool MatchesSearch(string searchString);

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            ClipboardObject other = obj as ClipboardObject;
            if (other != null)
            {
                return Name.ToLower().CompareTo(other.Name.ToLower());
            }
            else
            {
                throw new ArgumentException("Object is not a ClipboardObject");
            }
        }
    }

    public class TextClipboardObject : ClipboardObject
    {
        protected string text;

        public TextClipboardObject(MainWindow parentWindow, string label, string text)
            : base(parentWindow, label)
        {
            this.text = text;
        }

        public TextClipboardObject(MainWindow parentWindow, long id, string name, string content)
            : base(parentWindow, id, name)
        {
            text = content;
        }

        public override bool MatchesClipboard()
        {
            return Clipboard.ContainsText() && Clipboard.GetText() == text;
        }

        protected override void CopyContentToClipboard()
        {
            Console.WriteLine("copying text to clipboard");
            Clipboard.SetText(text);
        }

        public override FrameworkElement GenerateClipboardPreviewPanel()
        {
            TextBlock preview = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Background = new SolidColorBrush(Colors.White)
            };

            string[] lines = text.Replace("\t", "&#x9;").Split('\n');

            for (int i = 0; i < lines.Length; ++i)
            {
                preview.Inlines.Add(new Run(lines[i]));
            }

            return preview;
        }

        public override string GenerateContentString()
        {
            return text;
        }

        protected override DataObject GetDataObject()
        {
            return new DataObject(DataFormats.Text, text);
        }

        public override bool MatchesSearch(string searchString)
        {
            return Name.ToLower().Contains(searchString) || text.ToLower().Contains(searchString);
        }
    }

    public class ImageClipboardObject : ClipboardObject
    {
        protected string localPath;
        protected SerializableDataObject dataObject;

        public ImageClipboardObject(MainWindow parentWindow, string label, IDataObject dataObject)
            : base(parentWindow, label)
        {
            localPath = "";
            this.dataObject = new SerializableDataObject(dataObject);
        }

        public ImageClipboardObject(MainWindow parentWindow, long id, string name, string content)
            : base(parentWindow, id, name)
        {
            localPath = content;
            dataObject = SerializableDataObject.LoadFromFile(localPath);
            UsesInternalStorage = true;
        }

        public override bool MatchesClipboard() // TODO: fix equality check
        {
            return Clipboard.ContainsImage() && BitmapExtension.GetImageFromDataObject(new SerializableDataObject(Clipboard.GetDataObject()).GetDataObject()).ToBytes().SequenceEqual(BitmapExtension.GetImageFromDataObject(dataObject.GetDataObject()).ToBytes());
        }

        protected override void CopyContentToClipboard()
        {
            Clipboard.SetDataObject(dataObject.GetDataObject());
        }

        public override FrameworkElement GenerateClipboardPreviewPanel()
        {
            return new Image
            {
                Source = BitmapExtension.ImageToBitMapImage(BitmapExtension.GetImageFromDataObject(dataObject.GetDataObject())),
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        public override string GenerateContentString()
        {
            if (localPath == "")
            {
                localPath = Paths.LOCAL_STORAGE_FILE_PATH(Guid.NewGuid().ToString());
            }
            if (!File.Exists(localPath))
            {
                dataObject.SaveToFile(localPath);
                UsesInternalStorage = true;
            }
            return localPath;
        }

        protected override DataObject GetDataObject()
        {
            return dataObject.GetDataObject();
        }

        public override bool MatchesSearch(string searchString)
        {
            return Name.ToLower().Contains(searchString);
        }
    }

    public class FileDropListClipboardObject : ClipboardObject
    {
        protected StringCollection fileDropList;

        public FileDropListClipboardObject(MainWindow parentWindow, string label, StringCollection fileDropList)
            : base(parentWindow, label)
        {
            this.fileDropList = fileDropList;
        }

        public FileDropListClipboardObject(MainWindow parentWindow, long id, string name, string content)
            : base(parentWindow, id, name)
        {
            fileDropList = new StringCollection();
            fileDropList.AddRange(content.Split(';'));
        }

        public override bool MatchesClipboard()
        {
            if (Clipboard.ContainsFileDropList())
            {
                StringCollection clipboardFileDropList = Clipboard.GetFileDropList();
                if (clipboardFileDropList.Count == fileDropList.Count)
                {
                    foreach (string filepath in fileDropList)
                    {
                        if (!clipboardFileDropList.Contains(filepath))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        protected override void CopyContentToClipboard()
        {
            Clipboard.SetFileDropList(fileDropList);
        }

        public override FrameworkElement GenerateClipboardPreviewPanel()
        {
            UniformGrid preview = new UniformGrid();

            foreach (string filepath in fileDropList)
            {
                Image thumbnail = new Image
                {
                    Source = Images.LOADING,
                    Height = 150,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                Dispatcher.CurrentDispatcher.BeginInvoke((Action) (() => {
                    thumbnail.ToolTip = filepath;
                    try
                    {
                        thumbnail.Source = GetFileThumbnail(filepath).Result;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("unhandled! " + e.Message);
                        thumbnail.ToolTip = e.Message;
                    }
                }));
                preview.Children.Add(thumbnail);
            }
            return preview;
        }

        private static async Task<BitmapImage> GetFileThumbnail(string filepath)
        {
            try
            {
                StorageItemThumbnail thumbnail;
                FileAttributes attr = File.GetAttributes(@filepath);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    StorageFolder folder = StorageFolder.GetFolderFromPathAsync(@filepath).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    thumbnail = await folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 150).AsTask().ConfigureAwait(false);
                }
                else
                {
                    StorageFile file = StorageFile.GetFileFromPathAsync(@filepath).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 150).AsTask().ConfigureAwait(false);
                }
                BitmapImage image = new BitmapImage();
                using (Stream stream = thumbnail.AsStreamForRead())
                {
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();
                }
                return image;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("invalid permission on " + filepath);
                return Images.UNAUTHORIZED;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("file not found: " + filepath);
                return Images.MISSING;
            }
        }

        public override string GenerateContentString()
        {
            string[] strArr = new string[fileDropList.Count];
            fileDropList.CopyTo(strArr, 0);
            return string.Join(";", strArr);
        }

        protected override DataObject GetDataObject()
        {
            string[] strArr = new string[fileDropList.Count];
            fileDropList.CopyTo(strArr, 0);
            return new DataObject(DataFormats.FileDrop, strArr);
        }

        public override bool MatchesSearch(string searchString)
        {
            if (Name.ToLower().Contains(searchString))
            {
                return true;
            }
            foreach (string fileName in fileDropList)
            {
                if (fileName.ToLower().Contains(searchString))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class Reversed : IComparer<ClipboardObject>
    {
        public int Compare(ClipboardObject o1, ClipboardObject o2)
        {
            return o2.CompareTo(o1);
        }
    }
}
