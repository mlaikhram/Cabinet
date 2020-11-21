using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Cabinet
{
    public abstract class ClipboardObject
    {
        private MainWindow parentWindow;
        private StackPanel stackPanel;
        private Border clipboardContainer;
        public Border ClipboardContainer {
            get
            {
                if (clipboardContainer.Child == null)
                {
                    AddClipboardPreviewPanelToStackPanel();
                    clipboardContainer.Child = stackPanel;
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

            stackPanel = new StackPanel();
            stackPanel.Children.Add(new Label
            {
                Content = label,
                Background = (SolidColorBrush) new BrushConverter().ConvertFrom("#FF818181"),
                Foreground = (SolidColorBrush) new BrushConverter().ConvertFrom("#FFFFFFFF"),
                FontSize = 8,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0)
            });

            clipboardContainer = new Border
            {
                Width = 132,
                Height = 164,
                Margin = new Thickness(6, 6, 0, 0),
                BorderBrush = (SolidColorBrush) new BrushConverter().ConvertFrom("#FF666666"),
                BorderThickness = new Thickness(2),
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
}
