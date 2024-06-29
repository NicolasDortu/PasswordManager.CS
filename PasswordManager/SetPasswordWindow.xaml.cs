using System.Windows;

namespace PasswordManagerWPF
{
    public partial class SetPasswordWindow : Window
    {
        public string NewPassword { get; private set; }

        public SetPasswordWindow()
        {
            InitializeComponent();
        }

        private void SetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (NewPasswordBox.Password == ConfirmPasswordBox.Password)
            {
                NewPassword = NewPasswordBox.Password;
                DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Passwords do not match. Please try again.");
            }
        }
    }
}
