using System.Windows;

namespace PasswordManagerWPF
{
    public partial class PasswordDisplayWindow : Window
    {
        public PasswordDisplayWindow(string generatedPassword)
        {
            InitializeComponent();
            PasswordTextBox.Text = generatedPassword;
            PasswordTextBox.SelectAll();
            PasswordTextBox.Focus();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(PasswordTextBox.Text);
            CustomMessageBox.Show("Password copied to clipboard!");
        }
    }
}
