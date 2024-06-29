using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
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
        }

        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            string appName = Microsoft.VisualBasic.Interaction.InputBox("Enter the app name:", "App Name", "Default");
            GenerateAndStorePassword(appName, mainPassword);
        }

        private void GenerateAndStorePassword(string appName, string mainPassword)
        {
            string newPassword = GenerateRandomPassword();
            byte[] encryptedPassword = Encrypt(newPassword, mainPassword);

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Passwords (UserID, AppName, EncryptedPassword) VALUES (1, @appName, @encryptedPassword)";
                command.Parameters.AddWithValue("@appName", appName);
                command.Parameters.AddWithValue("@encryptedPassword", encryptedPassword);
                command.ExecuteNonQuery();
            }

            MessageBox.Show($"Generated password for {appName}: {newPassword}");
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

        private void ViewPasswords_Click(object sender, RoutedEventArgs e)
        {
            ViewStoredPasswords(mainPassword);
        }

        private void ViewStoredPasswords(string mainPassword)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT AppName, EncryptedPassword FROM Passwords WHERE UserID = 1";
                using (var reader = command.ExecuteReader())
                {
                    PasswordListBox.Items.Clear();
                    while (reader.Read())
                    {
                        string appName = reader.GetString(0);
                        byte[] encryptedPassword = (byte[])reader[1];
                        string decryptedPassword = Decrypt(encryptedPassword, mainPassword);

                        PasswordListBox.Items.Add($"App: {appName}, Password: {decryptedPassword}");
                    }
                }
            }
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
