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
    /// Interaction logic for ClipboardObjectForm.xaml
    /// </summary>
    public partial class ClipboardObjectForm : UserControl
    {
        public MainWindow ParentWindow { get; set; }

        private FormType FormType { get; set; }
        private ClipboardObject clipboardObject;
        private Category category;

        public ClipboardObjectForm()
        {
            InitializeComponent();
            ClipboardObjectName.MaxLength = FormConstants.CLIP_NAME_LIMIT;
        }

        public void OpenCreateForm(Category category, ClipboardObject clipboardObject)
        {
            FormType = FormType.CREATE;

            this.category = category;
            this.clipboardObject = clipboardObject;

            TitleLabel.Content = "Save to " + (category.Name.Length > FormConstants.ADD_TO_CATEGORY_NAME_LIMIT ? category.Name.Substring(0, FormConstants.ADD_TO_CATEGORY_NAME_LIMIT) + "..." : category.Name);
            ClipboardObjectName.Text = clipboardObject.Name;
            ClipboardPanel.Children.Clear();
            FrameworkElement previewPanel = clipboardObject.GenerateClipboardPreviewPanel();
            previewPanel.Height = 150;
            ClipboardPanel.Children.Add(previewPanel);

            Visibility = Visibility.Visible;
        }

        public void OpenUpdateForm(Category category, ClipboardObject clipboardObject)
        {
            FormType = FormType.EDIT;

            this.category = category;
            this.clipboardObject = clipboardObject;

            TitleLabel.Content = "Edit Clip";
            ClipboardObjectName.Text = clipboardObject.Name;
            ClipboardPanel.Children.Clear();
            FrameworkElement previewPanel = clipboardObject.GenerateClipboardPreviewPanel();
            previewPanel.Height = 150;
            ClipboardPanel.Children.Add(previewPanel);

            Visibility = Visibility.Visible;
        }

        private void SubmitClipboardObjectForm(object sender, MouseButtonEventArgs e)
        {
            if (ClipboardObjectName.Text.Trim() == "")
            {
                ClipboardObjectName.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.ERROR);
                NameErrorText.Content = "Name is required";
            }
            else if (NameErrorText.Content.ToString().Trim() == "")
            {
                // TODO: display loading overlay
                try
                {
                    string name = ClipboardObjectName.Text.Trim();

                    if (FormType == FormType.CREATE)
                    {
                        long categoryId = category.Id;
                        string type = clipboardObject.GetType().Name;
                        string content = clipboardObject.GenerateContentString();
                        long id = DBManager.Instance.AddClipboardObject(categoryId, name, type, content);
                        category.AddClipboardObject(ClipboardObjectUtils.CreateClipboardObjectByType(ParentWindow, id, name, type, content));
                    }
                    else if (clipboardObject != null)
                    {
                        DBManager.Instance.UpdateClipboardObject(clipboardObject.Id, name);
                        clipboardObject.Name = name;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("could not save/update clipboard object: " + ex.Message);
                    // TODO: show error in overlay
                }
                CloseForm(sender, e);
                // TODO: close loading overlay
            }
        }

        private void CloseForm(object sender, MouseButtonEventArgs e)
        {
            category = null;
            clipboardObject = null;
            ClipboardObjectName.Text = "";
            ClipboardObjectName.BorderBrush = new SolidColorBrush(Colors.White);
            NameErrorText.Content = "";

            Visibility = Visibility.Hidden;
        }

        private void ClipboardObjectName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (category != null && clipboardObject != null)
            {
                if (category.Status == LoadStatus.UNLOADED)
                {
                    ClipboardObjectName.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.LOADING);
                    NameErrorText.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.LOADING);
                    NameErrorText.Content = "Loading saved clips...";
                    Dispatcher.BeginInvoke((Action)(() => {
                        IEnumerable<ClipboardObject> loader = category.ClipboardObjects;
                        ClipboardObjectName_TextChanged(sender, e);
                    }));
                }
                else if (category.Status == LoadStatus.LOADED)
                {
                    ClipboardObject sameNameClipboardObject = category.ClipboardObjects.FirstOrDefault((clipboard) => clipboard.Name == ClipboardObjectName.Text.Trim());
                    if (sameNameClipboardObject != null && (FormType == FormType.CREATE || (clipboardObject != null && sameNameClipboardObject.Id != clipboardObject.Id)))
                    {
                        ClipboardObjectName.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.ERROR);
                        NameErrorText.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(ColorSet.ERROR);
                        NameErrorText.Content = "Name already taken";
                    }
                    else if (ClipboardObjectName.Text.Trim() != "")
                    {
                        ClipboardObjectName.BorderBrush = new SolidColorBrush(Colors.White);
                        NameErrorText.Content = "";
                    }
                }
            }
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background.Opacity = 0.5;
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background.Opacity = 1.0;
        }
    }
}