﻿using System;
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

        private readonly LinkedList<ClipboardObject> clipboardObjects;
        public IEnumerable<ClipboardObject> ClipboardObjects
        {
            get
            {
                if (!isLoaded)
                {
                    // TODO: load all clipboard objects from db
                    Console.WriteLine("initial load from DB for " + Name);
                    isLoaded = true;
                }

                return clipboardObjects.AsEnumerable();
            }
        }

        public Border Icon { get; private set; }
        public Image IconImage { get; private set; }

        private bool isLoaded;

        public Category(MainWindow parentWindow) : this(parentWindow, Recent.ID, Recent.NAME, Recent.ICON_PATH, Colors.AntiqueWhite)
        {
            isLoaded = true;
        }

        public Category(MainWindow parentWindow, long id, string name, string iconPath, Color color)
        {
            Id = id;
            Name = name;
            IconPath = iconPath;
            Color = color;

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

            Icon.Child = IconImage;
            Icon.MouseEnter += (sender, e) => IconImage.Margin = new Thickness(5);
            Icon.MouseLeave += (sender, e) => IconImage.Margin = new Thickness(10);
            Icon.MouseLeftButtonUp += (sender, e) => parentWindow.SetActiveCategory(Id);
            Icon.Drop += OpenAddClipboardObjectForm; // TODO: pull up add clipboardObject form

            isLoaded = false;
        }

        public void OpenAddClipboardObjectForm(object sender, DragEventArgs e)
        {
            DataObject draggedObject = (DataObject)e.Data;
            ClipboardObject clipboardObject = (ClipboardObject)draggedObject.GetData("ClipboardObject");
            Console.WriteLine("dropped " + clipboardObject.Label + " on " + Name);
        }

        public void AddClipboardObject(ClipboardObject clipboardObject, bool updateDB = true)
        {
            clipboardObjects.AddFirst(clipboardObject);
            // TODO: db update if not recents
        }

        public void RemoveClipboardObject(ClipboardObject clipboardObject, bool updateDB = true)
        {
            clipboardObjects.Remove(clipboardObject);
            // TODO: db update if not recents
        }
    }
}
