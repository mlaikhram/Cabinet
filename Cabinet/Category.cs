using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Graphics.Printing.OptionDetails;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;

namespace Cabinet
{
    public class Category
    {
        public long Id { get; private set; }
        public string Name { get; private set; }
        public string IconPath { get; private set; }
        public Color Color { get; private set; }

        private MainWindow parentWindow;

        private LinkedList<ClipboardObject> clipboardObjects;
        public IEnumerable<ClipboardObject> ClipboardObjects
        {
            get
            {
                if (Status == LoadStatus.UNLOADED)
                {
                    Status = LoadStatus.LOADING;
                    clipboardObjects = new LinkedList<ClipboardObject>(DBManager.Instance.GetClipboardObjects(parentWindow, Id));
                    Console.WriteLine("initial load from DB for " + Name);
                    Status = LoadStatus.LOADED;
                }

                return clipboardObjects.AsEnumerable();
            }
        }

        public Border Icon { get; private set; }
        public Image IconImage { get; private set; }

        public LoadStatus Status { get; private set; }

        public Category(MainWindow parentWindow) : this(parentWindow, Recent.ID, Recent.NAME, Recent.ICON_PATH, Colors.AntiqueWhite)
        {
            Status = LoadStatus.LOADED;
        }

        public Category(MainWindow parentWindow, long id, string name, string iconPath, Color color)
        {
            Id = id;
            Name = name;
            IconPath = iconPath;
            Color = color;

            this.parentWindow = parentWindow;
            clipboardObjects = new LinkedList<ClipboardObject>();

            Icon = new Border
            {
                Margin = new Thickness(10, Id >= 0 ? 5 : 10, 10, 5),
                BorderBrush = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(10),
                Height = 60,
                Width = 60,
                Background = new SolidColorBrush(Color),
                AllowDrop = Id >= 0
            };

            IconImage = new Image
            {
                Margin = new Thickness(10),
                Source = new BitmapImage(new Uri(IconPath, UriKind.RelativeOrAbsolute))
            };

            MenuItem updateItem = new MenuItem
            {
                Header = "Edit"
            };
            updateItem.Click += (sender, e) => parentWindow.CategoryForm.OpenUpdateForm(this);

            MenuItem deleteItem = new MenuItem
            {
                Header = "Delete"
            };
            // TODO: attach click handler

            Icon.ContextMenu = new ContextMenu(); // TODO: proper styling
            Icon.ContextMenu.Items.Add(updateItem);
            Icon.ContextMenu.Items.Add(deleteItem);

            Icon.Child = IconImage;
            Icon.MouseEnter += (sender, e) => IconImage.Margin = new Thickness(5);
            Icon.MouseLeave += (sender, e) => IconImage.Margin = new Thickness(10);
            Icon.MouseLeftButtonUp += (sender, e) => parentWindow.SetActiveCategory(Id);
            Icon.Drop += OpenAddClipboardObjectForm;

            Status = LoadStatus.UNLOADED;
        }

        public void UpdateCategory(string name, string iconPath, Color color)
        {
            Name = name;

            IconPath = iconPath;
            IconImage.Source = new BitmapImage(new Uri(IconPath, UriKind.RelativeOrAbsolute));

            Color = color;
            Icon.Background = new SolidColorBrush(Color);
        }

        public void OpenAddClipboardObjectForm(object sender, DragEventArgs e)
        {
            if (parentWindow.CurrentCategoryId != Id)
            {
                ClipboardObject clipboardObject = (ClipboardObject)e.Data.GetData("ClipboardObject");
                Console.WriteLine("dropped " + clipboardObject.Name + " on " + Name);
                parentWindow.ClipboardForm.OpenForm(this, clipboardObject);
            }
            else
            {
                Console.WriteLine("cannot save to active category");
            }
        }

        public void AddClipboardObject(ClipboardObject clipboardObject)
        {
            clipboardObjects.AddFirst(clipboardObject);
        }

        public void RemoveClipboardObject(ClipboardObject clipboardObject)
        {
            clipboardObjects.Remove(clipboardObject);
        }
    }
}
