using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Cabinet
{
    public static class Paths
    {
        public static string[] ICONS => Directory.GetFiles(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Icons"), @"*.png");
        public static string ICON_PATH(string name) => Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Icons", name + ".png");
        public static string LOCAL_IMAGE_CLIP_PATH(string name) => Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Clips", name); // TODO: create Clips folder if it doesn't exist // TODO: create method to optimize folder by merging duplicate files
    }

    public static class Images
    {
        public static Icon LOGO => Properties.Resources.logo;
        public static BitmapImage LOADING => BitmapConverter.ToBitmapImage(Properties.Resources.hourglass);
        public static BitmapImage MISSING => BitmapConverter.ToBitmapImage(Properties.Resources.broken);
        public static BitmapImage UNAUTHORIZED => BitmapConverter.ToBitmapImage(Properties.Resources._lock);
    }

    public static class Recent
    {
        public static readonly int ID = -1;
        public static readonly string NAME = "Recent";
        public static readonly string ICON_PATH = "/Cabinet;component/Images/recent.png";

        public static readonly string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";
    }

    public static class ColorSet
    {
        public static readonly string ERROR = "#FF9C0404";
        public static readonly string LOADING = "#FF0592A8";

        public static readonly string CLIPBOARD_LABEL_BG = "#FF818181";
        public static readonly string CLIPBOARD_BORDER = "#FF666666";
    }

    public enum LoadStatus
    {
        UNLOADED,
        LOADING,
        LOADED
    }

    public static class ClipboardObjectUtils
    {
        public static ClipboardObject CreateClipboardObjectByType(MainWindow parentWindow, long id, string name, string type, string content)
        {
            switch (type)
            {
                case "TextClipboardObject":
                    return new TextClipboardObject(parentWindow, id, name, content);

                case "ImageClipboardObject":
                    return new ImageClipboardObject(parentWindow, id, name, content);

                case "FileDropListClipboardObject":
                    return new FileDropListClipboardObject(parentWindow, id, name, content);

                default:
                    return null;
            }
        }
    }

    public static class BitmapConverter
    {
        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
    }

    public static class ControlUtils
    {
        public static ContextMenu CreateContextMenu()
        {
            return new ContextMenu
            {
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF121212"),
                Margin = new Thickness(-33, 0, 0, 0),
                BorderThickness = new Thickness(0)
            };
        }

        public static MenuItem CreateMenuItem(string header)
        {
            return new MenuItem
            {
                Header = header,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(33, 0, 0, 0)
            };
        }

        public static Border CreateCategoryIcon(string iconPath, System.Windows.Media.Color color, Thickness margin, out System.Windows.Controls.Image iconImage)
        {
            iconImage = new System.Windows.Controls.Image
            {
                Margin = new Thickness(10),
                Source = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute))
            };

            Border icon = new Border
            {
                Margin = margin,
                BorderBrush = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(10),
                Height = 60,
                Width = 60,
                Background = new SolidColorBrush(color),
            };

            icon.Child = iconImage;

            return icon;
        }

        public delegate FrameworkElement PreviewPanelGenerator();
        public static Border CreateClipboardObjectContainer(string name, Thickness margin, PreviewPanelGenerator previewPanelGenerator, out Label label, out StackPanel stackPanel)
        {
            label = new Label
            {
                Content = name,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.CLIPBOARD_LABEL_BG),
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 8,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0)
            };

            stackPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Bottom
            };

            stackPanel.Children.Add(label);

            Border container = new Border
            {
                Width = 132,
                Height = 164,
                Margin = margin,
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.CLIPBOARD_BORDER),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Colors.Transparent)
            };

            container.Child = stackPanel;

            if (previewPanelGenerator != null)
            {
                FrameworkElement previewPanel = previewPanelGenerator();
                previewPanel.Height = 150;
                stackPanel.Children.Insert(0, previewPanel);
            }

            return container;
        }

        public static TextBlock CreateConfirmationText(string text)
        {
            return new TextBlock
            {
                Margin = new Thickness(10, 0, 10, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.WrapWithOverflow,
                FontSize = 18,
                Foreground = new SolidColorBrush(Colors.White),
                Text = text
            };
        }
    }
}