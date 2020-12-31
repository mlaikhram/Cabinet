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
    /// Interaction logic for ConfirmationForm.xaml
    /// </summary>
    public partial class ConfirmationForm : UserControl
    {
        public delegate void FormAction();

        private FormAction formAction;

        public ConfirmationForm()
        {
            InitializeComponent();
            formAction = null;
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background.Opacity = 0.5;
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background.Opacity = 1.0;
        }

        public void OpenForm(string title, FormAction formAction, params UIElement[] content)
        {
            this.formAction = formAction;
            TitleLabel.Content = title;
            ContentPanel.Children.Clear();
            foreach (UIElement element in content)
            {
                ContentPanel.Children.Add(element);
            }

            Visibility = Visibility.Visible;
        }

        private void CloseForm(object sender, MouseButtonEventArgs e)
        {
            TitleLabel.Content = "";
            ContentPanel.Children.Clear();
            formAction = null;

            Visibility = Visibility.Hidden;
        }

        private void Confirm(object sender, MouseButtonEventArgs e)
        {
            formAction?.Invoke();
            CloseForm(sender, e);
        }
    }
}
