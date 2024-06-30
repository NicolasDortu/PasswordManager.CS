using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace PasswordManagerWPF
{
    public partial class PasswordManagerWindow : Window
    {
        private string connectionString = "Data Source=passwords.db";
        private string mainPassword;

        public PasswordManagerWindow(string password)
        {
            InitializeComponent();
            mainPassword = password;
            LoadStoredPasswords();
        }

        private void LoadStoredPasswords()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT PasswordID, AppName FROM Passwords WHERE UserID = 1";
                using (var reader = command.ExecuteReader())
                {
                    PasswordListBox.Items.Clear();
                    while (reader.Read())
                    {
                        var item = new ListBoxItem
                        {
                            Content = reader.GetString(reader.GetOrdinal("AppName")),
                            Tag = reader.GetInt32(reader.GetOrdinal("PasswordID"))
                        };
                        PasswordListBox.Items.Add(item);
                    }
                }
            }
        }

        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            string appName = Microsoft.VisualBasic.Interaction.InputBox("Enter the app name:", "App Name", "Default");
            string login = Microsoft.VisualBasic.Interaction.InputBox("Enter the login (optional):", "Login", "");
            GenerateAndStorePassword(appName, login, mainPassword);
        }

        private void GenerateAndStorePassword(string appName, string login, string mainPassword)
        {
            string newPassword = GenerateRandomPassword();
            byte[] encryptedPassword = Encrypt(newPassword, mainPassword);

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Passwords (UserID, AppName, Login, EncryptedPassword) VALUES (1, @appName, @login, @encryptedPassword)";
                command.Parameters.AddWithValue("@appName", appName);
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@encryptedPassword", encryptedPassword);
                command.ExecuteNonQuery();
            }

            MessageBox.Show($"Generated password for {appName}: {newPassword}");
            LoadStoredPasswords(); // Refresh the list
        }

        private void PasswordListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PasswordListBox.SelectedItem is ListBoxItem selectedItem)
            {
                int passwordId = (int)selectedItem.Tag;
                OpenAppDetailsWindow(passwordId);
            }
        }

        private void OpenAppDetailsWindow(int passwordId)
        {
            AppDetailsWindow appDetailsWindow = new AppDetailsWindow(passwordId, mainPassword, connectionString);
            appDetailsWindow.Closed += (s, e) => LoadStoredPasswords();
            appDetailsWindow.ShowDialog();
        }

        private string GenerateRandomPassword()
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            StringBuilder res = new StringBuilder();
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];

                while (res.Length < 16)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    res.Append(validChars[(int)(num % (uint)validChars.Length)]);
                }
            }

            return res.ToString();
        }

        private byte[] Encrypt(string text, string password)
        {
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("SaltIsGoodForYou")).GetBytes(32);
                aes.Key = key;
                aes.IV = new byte[16];

                using (var encryptor = aes.CreateEncryptor())
                {
                    var bytes = Encoding.UTF8.GetBytes(text);
                    return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                }
            }
        }

        private string Decrypt(byte[] data, string password)
        {
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("SaltIsGoodForYou")).GetBytes(32);
                aes.Key = key;
                aes.IV = new byte[16];

                using (var decryptor = aes.CreateDecryptor())
                {
                    var result = decryptor.TransformFinalBlock(data, 0, data.Length);
                    return Encoding.UTF8.GetString(result);
                }
            }
        }
    }
}
