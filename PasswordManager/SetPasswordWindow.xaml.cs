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

        private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SetPassword_Click(sender, e);
            }
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
                CustomMessageBox.Show("Passwords do not match. Please try again.");
            }
        }
    }
}
