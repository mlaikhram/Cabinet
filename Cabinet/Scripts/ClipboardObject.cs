﻿using System;
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
using FileAttributes = System.IO.FileAttributes;
using Image = System.Windows.Controls.Image;

namespace Cabinet
{
    public abstract class ClipboardObject
    {
        private static bool IS_DRAGGING = false;
        private static long NEXT_RECENT_ID = 0;

        public long Id { get; private set; }

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
                if (clipboardContainer.Child == null)
                {
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

        protected ClipboardObject(MainWindow parentWindow, string label) : this(parentWindow, NEXT_RECENT_ID++, label)
        {
        }

        protected ClipboardObject(MainWindow parentWindow, long id, string label)
        {
            Id = id;
            UsesInternalStorage = false;
            this.parentWindow = parentWindow;
            this.label = new Label
            {
                Content = label,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.CLIPBOARD_LABEL_BG),
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 8,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0)
            };

            StackPanel = new StackPanel();
            StackPanel.Children.Add(this.label);

            clipboardContainer = new Border
            {
                Width = 132,
                Height = 164,
                Margin = new Thickness(6, 0, 0, 6),
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.CLIPBOARD_BORDER),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Colors.Transparent)
            };

            MenuItem updateItem = new MenuItem
            {
                Header = "Edit",
                IsEnabled = false
            };
            //updateItem.Click += (sender, e) => parentWindow.CategoryForm.OpenUpdateForm(this);

            MenuItem deleteItem = new MenuItem
            {
                Header = "Delete"
            };
            deleteItem.Click += (sender, e) => parentWindow.DeleteClipboardObject(Id);

            clipboardContainer.ContextMenu = new ContextMenu(); // TODO: proper styling
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
            return Name.ToLower().Contains(searchString);
        }
    }

    public class ImageClipboardObject : ClipboardObject // TODO: format image correctly to keep transparency
    {
        protected string localPath;
        protected System.Drawing.Image image;

        public ImageClipboardObject(MainWindow parentWindow, string label, System.Drawing.Image image)
            : base(parentWindow, label)
        {
            localPath = "";
            this.image = image;
        }

        public ImageClipboardObject(MainWindow parentWindow, long id, string name, string content)
            : base(parentWindow, id, name)
        {
            localPath = content;
            using (Bitmap temp = new Bitmap(content))
            {
                image = new Bitmap(temp);
                //image = System.Drawing.Image.FromFile(content);
            }
            //catch (UnauthorizedAccessException)
            //{
            //    Console.WriteLine("invalid permission on " + content);
            //    image = Images.UNAUTHORIZED;
            //}
            //catch (FileNotFoundException)
            //{
            //    Console.WriteLine("file not found: " + content);
            //    image = Images.MISSING;
            //}
            UsesInternalStorage = true;
        }

        public override bool MatchesClipboard()
        {
            return Clipboard.ContainsImage() && AreEqual(new Bitmap(Clipboard.GetImage()), new Bitmap(image));
        }

        protected override void CopyContentToClipboard()
        {
            Clipboard.SetImage(image);
        }

        public override FrameworkElement GenerateClipboardPreviewPanel()
        {
            return new Image
            {
                Source = ImageToBitMapImage(image),
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private BitmapImage ImageToBitMapImage(System.Drawing.Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        private unsafe bool AreEqual(Bitmap b1, Bitmap b2)
        {
            if (b1.Size != b2.Size)
            {
                return false;
            }

            if (b1.PixelFormat != b2.PixelFormat)
            {
                return false;
            }

            if (b1.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                return false;
            }

            Rectangle rect = new Rectangle(0, 0, b1.Width, b1.Height);
            BitmapData data1
                = b1.LockBits(rect, ImageLockMode.ReadOnly, b1.PixelFormat);
            BitmapData data2
                = b2.LockBits(rect, ImageLockMode.ReadOnly, b1.PixelFormat);

            int* p1 = (int*)data1.Scan0;
            int* p2 = (int*)data2.Scan0;
            int byteCount = b1.Height * data1.Stride / 4; //only Format32bppArgb 

            bool result = true;
            for (int i = 0; i < byteCount; ++i)
            {
                if (*p1++ != *p2++)
                {
                    result = false;
                    break;
                }
            }

            b1.UnlockBits(data1);
            b2.UnlockBits(data2);

            return result;
        }

        public override string GenerateContentString()
        {
            if (localPath == "")
            {
                localPath = Paths.LOCAL_IMAGE_CLIP_PATH(Guid.NewGuid().ToString());
            }
            if (!File.Exists(localPath))
            {
                image.Save(localPath, image.RawFormat);
                UsesInternalStorage = true;
            }
            return localPath;
        }

        protected override DataObject GetDataObject()
        {
            return new DataObject(DataFormats.Bitmap, image);
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
            return Name.ToLower().Contains(searchString);
        }
    }
}