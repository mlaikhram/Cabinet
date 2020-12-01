using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cabinet
{
    /// <summary>
    /// Interaction logic for CategoryForm.xaml
    /// </summary>
    public partial class CategoryForm : UserControl
    {
        public MainWindow MainWindow { get; set; }

        private FormType FormType { get; set; }

        public CategoryForm()
        {
            InitializeComponent();
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            IconBorder.Background = new SolidColorBrush(e.NewValue.GetValueOrDefault());
        }

        private void SelectedIcon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: update icon preview
            Console.WriteLine("updating icon to " + ((ComboBoxItem) e.AddedItems[0]).Content);
        }

        private void CategoryName_TextChanged(object sender, TextChangedEventArgs e)
        {
            // TODO: check if name already exists, and if updating, ignore self matches
            if (CategoryName.Text.Contains("test"))
            {
                CategoryName.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF9C0404");
                NameErrorText.Content = "Name already taken";
            }
            else
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
            // IconPreview = 
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
            // TODO: update db and add category to sidebar
            Console.WriteLine("submit");
            CloseForm(sender, e);
        }
    }

    internal enum FormType
    {
        CREATE,
        EDIT
    }
}
