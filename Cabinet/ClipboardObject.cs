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
        private MainWindow parentWindow;
        private Label label;
        public string Label
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

        private StackPanel stackPanel;
        private Border clipboardContainer;
        public Border ClipboardContainer {
            get
            {
                if (clipboardContainer.Child == null)
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() =>
                    {
                        AddClipboardPreviewPanelToStackPanel();
                        clipboardContainer.Child = stackPanel;
                    }));
                }
                return clipboardContainer;
            }
            private set
            {
                clipboardContainer = value;
            }
        }

        protected ClipboardObject(MainWindow parentWindow, string label)
        {
            this.parentWindow = parentWindow;
            this.label = new Label
            {
                Content = label,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF818181"),
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFFFFF"),
                FontSize = 8,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0)
            };

            stackPanel = new StackPanel();
            stackPanel.Children.Add(this.label);

            clipboardContainer = new Border
            {
                Width = 132,
                Height = 164,
                Margin = new Thickness(6, 6, 0, 0),
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF666666"),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            clipboardContainer.AddHandler(UIElement.MouseLeftButtonUpEvent, new RoutedEventHandler(TriggerClipboardCopy), true);
            clipboardContainer.AddHandler(UIElement.MouseEnterEvent, new RoutedEventHandler(OnHoverEnter), true);
            clipboardContainer.AddHandler(UIElement.MouseLeaveEvent, new RoutedEventHandler(OnHoverExit), true);
        }

        private void AddClipboardPreviewPanelToStackPanel()
        {
            FrameworkElement clipboardPreviewPanel = GenerateClipboardPreviewPanel();
            clipboardPreviewPanel.Height = 150;
            stackPanel.Children.Insert(0, clipboardPreviewPanel);
        }

        private void TriggerClipboardCopy(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("triggering copy");
            parentWindow.IncomingSelfCopy();
            CopyContentToClipboard();
            parentWindow.HideWindow();
        }

        protected virtual void OnHoverEnter(object sender, RoutedEventArgs e)
        {
            ClipboardContainer.BorderBrush = new SolidColorBrush(Colors.White);
        }

        protected virtual void OnHoverExit(object sender, RoutedEventArgs e)
        {
            ClipboardContainer.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF666666");
        }

        public abstract bool MatchesClipboard();
        protected abstract void CopyContentToClipboard();
        protected abstract FrameworkElement GenerateClipboardPreviewPanel();
    }

    public class TextClipboardObject : ClipboardObject
    {
        protected string text;

        public TextClipboardObject(MainWindow parentWindow, string label, string text)
            : base(parentWindow, label)
        {
            this.text = text;
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

        protected override FrameworkElement GenerateClipboardPreviewPanel()
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
    }

    public class ImageClipboardObject : ClipboardObject
    {
        protected System.Drawing.Image image;

        public ImageClipboardObject(MainWindow parentWindow, string label, System.Drawing.Image image)
            : base(parentWindow, label)
        {
            this.image = image;
        }

        public override bool MatchesClipboard()
        {
            return Clipboard.ContainsImage() && AreEqual(new Bitmap(Clipboard.GetImage()), new Bitmap(image));
        }

        protected override void CopyContentToClipboard()
        {
            Clipboard.SetImage(image);
        }

        protected override FrameworkElement GenerateClipboardPreviewPanel()
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

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, b1.Width, b1.Height);
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
    }

    public class FileDropListClipboardObject : ClipboardObject
    {
        protected StringCollection fileDropList;

        public FileDropListClipboardObject(MainWindow parentWindow, string label, StringCollection fileDropList)
            : base(parentWindow, label)
        {
            this.fileDropList = fileDropList;
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

        protected override FrameworkElement GenerateClipboardPreviewPanel()
        {
            UniformGrid preview = new UniformGrid();

            foreach (string filepath in fileDropList)
            {
                Image thumbnail = new Image
                {
                    Source = new BitmapImage(new Uri(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "settings.png"))),
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
                // TODO: set to unauthorizes BitmapImage
                return new BitmapImage(new Uri(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "settings.png")));
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("file not found: " + filepath);
                // TODO: set to missing file BitmapImage
                return new BitmapImage(new Uri(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "settings.png")));
            }
        }
    }
}
