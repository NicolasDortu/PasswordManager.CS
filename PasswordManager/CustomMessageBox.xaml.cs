using System.Windows;

namespace PasswordManagerWPF
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox(string message)
        {
            InitializeComponent();
            MessageTextBlock.Text = message;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public static bool Show(string message)
        {
            CustomMessageBox messageBox = new CustomMessageBox(message);
            return messageBox.ShowDialog() == true;
        }
    }
}


