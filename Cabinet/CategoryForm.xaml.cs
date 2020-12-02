using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cabinet
{
    /// <summary>
    /// Interaction logic for CategoryForm.xaml
    /// </summary>
    public partial class CategoryForm : UserControl
    {
        public MainWindow ParentWindow { get; set; }

        private FormType FormType { get; set; }

        public CategoryForm()
        {
            InitializeComponent();

            string[] icons = Directory.GetFiles(System.IO.Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Icons"), @"*.png");
            foreach (string icon in icons)
            {
                int startIndex = icon.LastIndexOf('\\') + 1;
                SelectedIcon.Items.Add(icon.Substring(startIndex, icon.LastIndexOf('.') - startIndex));
            }
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            IconBorder.Background = new SolidColorBrush(e.NewValue.GetValueOrDefault());
        }

        private void SelectedIcon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IconPreview.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Icons", e.AddedItems[0] + ".png")));
        }

        private void CategoryName_TextChanged(object sender, TextChangedEventArgs e)
        {
            Category sameNameCategory = ParentWindow.Categories.FirstOrDefault((category) => category.Name == CategoryName.Text.Trim());
            if (sameNameCategory != null && (FormType == FormType.CREATE || sameNameCategory.Id != ParentWindow.CurrentCategoryId))
            {
                CategoryName.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF9C0404");
                NameErrorText.Content = "Name already taken";
            }
            else if (CategoryName.Text.Trim() != "")
            {
                CategoryName.BorderBrush = new SolidColorBrush(Colors.White);
                NameErrorText.Content = "";
            }
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background.Opacity = 0.5;
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background.Opacity = 1.0;
        }

        private void CloseForm(object sender, MouseButtonEventArgs e)
        {
            TitleLabel.Content = "";
            IconColorPicker.SelectedColor = Colors.White;
            IconBorder.Background = new SolidColorBrush(Colors.White);
            SelectedIcon.SelectedIndex = 0;
            CategoryName.Text = "";
            CategoryName.BorderBrush = new SolidColorBrush(Colors.White);
            NameErrorText.Content = "";

            Visibility = Visibility.Hidden;
        }

        public void CreateCategoryForm()
        {
            TitleLabel.Content = "Create Category";
            FormType = FormType.CREATE;

            Visibility = Visibility.Visible;
        }

        private void SubmitCreateCategory(object sender, MouseButtonEventArgs e)
        {
            if (CategoryName.Text.Trim() == "")
            {
                CategoryName.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF9C0404");
                NameErrorText.Content = "Name is required";
            }
            else if (NameErrorText.Content.ToString().Trim() == "")
            {
                // TODO: display loading overlay
                try
                {
                    // TODO: try insert into db
                    string name = CategoryName.Text.Trim();
                    string iconPath = System.IO.Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Icons", SelectedIcon.Text + ".png");
                    Color color = ((SolidColorBrush)IconBorder.Background).Color;
                    long id = DBManager.Instance.AddCategory(name, iconPath, color);

                    ParentWindow.AddCategory(new Category(ParentWindow, id, name, iconPath, color));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("could not create category: " + ex.Message);
                    // TODO: show error in overlay
                }
                CloseForm(sender, e);
                // TODO: close loading overlay
            }
        }
    }

    internal enum FormType
    {
        CREATE,
        EDIT
    }
}
