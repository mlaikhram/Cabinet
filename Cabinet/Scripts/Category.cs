using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using Image = System.Windows.Controls.Image;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Cabinet
{
    public class Category
    {
        private static bool IS_DRAGGING = false;

        public long Id { get; private set; }
        public bool IsRecent => Id == Recent.ID;
        public string Name { get; private set; }
        public string IconPath { get; private set; }
        public Color Color { get; private set; }

        private MainWindow parentWindow;

        private SortedSet<ClipboardObject> clipboardObjects;
        public IEnumerable<ClipboardObject> ClipboardObjects
        {
            get
            {
                if (Status == LoadStatus.UNLOADED)
                {
                    Status = LoadStatus.LOADING;
                    clipboardObjects = new SortedSet<ClipboardObject>(DBManager.Instance.GetClipboardObjects(parentWindow, Id));
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
            Image iconImage;
            Icon = ControlUtils.CreateCategoryIcon(IconPath, Color, new Thickness(10, !IsRecent ? 5 : 10, 10, 5), out iconImage);
            IconImage = iconImage;
            Icon.AllowDrop = !IsRecent;

            if (!IsRecent)
            {
                clipboardObjects = new SortedSet<ClipboardObject>();
                MenuItem updateItem = ControlUtils.CreateMenuItem("Edit");
                updateItem.Click += (sender, e) => parentWindow.CategoryForm.OpenUpdateForm(this);

                MenuItem deleteItem = ControlUtils.CreateMenuItem("Delete");
                deleteItem.Click += (sender, e) => parentWindow.ConfirmationForm.OpenForm(
                    "Confirm Delete",
                    () => parentWindow.DeleteCategory(Id),
                    ControlUtils.CreateCategoryIcon(IconPath, Color, new Thickness(20), out _),
                    ControlUtils.CreateConfirmationText(string.Format("Are you sure you want to delete {0}?", Name))
                );

                Icon.ContextMenu = ControlUtils.CreateContextMenu();
                Icon.ContextMenu.Items.Add(updateItem);
                Icon.ContextMenu.Items.Add(deleteItem);

                Icon.MouseMove += OnMouseDrag;
            }
            else
            {
                clipboardObjects = new SortedSet<ClipboardObject>(new Reversed());
                MenuItem clearItem = ControlUtils.CreateMenuItem("Clear");
                clearItem.Click += (sender, e) => parentWindow.ClearRecents();

                Icon.ContextMenu = ControlUtils.CreateContextMenu();
                Icon.ContextMenu.Items.Add(clearItem);
            }

            Icon.MouseEnter += (sender, e) => IconImage.Margin = new Thickness(5);
            Icon.MouseLeave += (sender, e) => IconImage.Margin = new Thickness(10);
            Icon.MouseLeftButtonUp += (sender, e) =>
            {
                if (!IS_DRAGGING)
                {
                    parentWindow.SetActiveCategory(Id);
                }
            };
            Icon.Drop += HandleDrop;

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

        private void OnMouseDrag(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !IS_DRAGGING)
            {
                IS_DRAGGING = true;
                Icon.Visibility = Visibility.Hidden;
                Console.WriteLine("dragging c");
                DataObject dataObject = new DataObject();
                dataObject.SetData("Category", this);
                DragDrop.DoDragDrop((Border)sender, dataObject, DragDropEffects.Copy | DragDropEffects.Move);
                Console.WriteLine("finished c");
                IS_DRAGGING = false;
                Icon.Visibility = Visibility.Visible;
            }
            // TODO: visual drag effect
        }

        public void HandleDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ClipboardObject") && parentWindow.CurrentCategoryId != Id)
            {
                ClipboardObject clipboardObject = (ClipboardObject)e.Data.GetData("ClipboardObject");
                Console.WriteLine("dropped " + clipboardObject.Name + " on " + Name);
                parentWindow.ClipboardForm.OpenCreateForm(this, clipboardObject);
            }
            else if (e.Data.GetDataPresent("Category"))
            {
                Category category = (Category)e.Data.GetData("Category");
                Console.WriteLine("dropped " + category.Name + " on " + Name);
                parentWindow.MoveCategory(category, this);

            }
            else
            {
                Console.WriteLine("invalid drop");
            }
        }

        public void AddClipboardObject(ClipboardObject clipboardObject)
        {
            clipboardObjects.Add(clipboardObject);
        }

        public void RemoveClipboardObject(ClipboardObject clipboardObject)
        {
            clipboardObjects.Remove(clipboardObject);
        }

        public void ClearClipboardObjects()
        {
            clipboardObjects.Clear();
        }
    }
}
