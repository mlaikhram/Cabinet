using System;
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

            string[] icons = Paths.ICONS;
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
            IconPreview.Source = new BitmapImage(new Uri(Paths.ICON_PATH(e.AddedItems[0].ToString())));
        }

        private void CategoryName_TextChanged(object sender, TextChangedEventArgs e)
        {
            Category sameNameCategory = ParentWindow.Categories.FirstOrDefault((category) => category.Name == CategoryName.Text.Trim());
            if (sameNameCategory != null && (FormType == FormType.CREATE || sameNameCategory.Id != ParentWindow.CurrentCategoryId))
            {
                CategoryName.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.ERROR);
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
                CategoryName.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.ERROR);
                NameErrorText.Content = "Name is required";
            }
            else if (NameErrorText.Content.ToString().Trim() == "")
            {
                // TODO: display loading overlay
                try
                {
                    // TODO: try insert into db
                    string name = CategoryName.Text.Trim();
                    string iconPath = Paths.ICON_PATH(SelectedIcon.Text);
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
