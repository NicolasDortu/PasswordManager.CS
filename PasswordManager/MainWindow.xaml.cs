using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace PasswordManagerWPF
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Data Source=passwords.db";

        public MainWindow()
        {
            Batteries.Init(); // Initialize SQLitePCL
            InitializeComponent();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            bool isNewDatabase = !File.Exists("passwords.db");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                if (isNewDatabase)
                {
                    // Create the database schema if it's a new database
                    command.CommandText = @"
                        CREATE TABLE Users (
                            UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT NOT NULL,
                            EncryptedConnectionText BLOB NOT NULL
                        );

                        CREATE TABLE Passwords (
                            PasswordID INTEGER PRIMARY KEY AUTOINCREMENT,
                            UserID INTEGER NOT NULL,
                            AppName TEXT NOT NULL,
                            Login TEXT,
                            EncryptedPassword BLOB NOT NULL,
                            FOREIGN KEY(UserID) REFERENCES Users(UserID)
                        );
                    ";
                    command.ExecuteNonQuery();
                }


                // Check if the EncryptedConnectionText is present
                command.CommandText = "SELECT EncryptedConnectionText FROM Users WHERE Username = 'admin'";
                var result = command.ExecuteScalar();

                if (result == null)
                {
                    // Prompt user to set a new password
                    CustomMessageBox.Show("No main password set. Please set a new main password.");
                    var setPasswordWindow = new SetPasswordWindow();
                    if (setPasswordWindow.ShowDialog() == true)
                    {
                        string newPassword = setPasswordWindow.NewPassword;
                        string connectionText = "You are connected";
                        byte[] encryptedConnectionText = Encrypt(connectionText, newPassword);

                        command.CommandText = "INSERT INTO Users (Username, EncryptedConnectionText) VALUES ('admin', @encryptedConnectionText)";
                        command.Parameters.AddWithValue("@encryptedConnectionText", encryptedConnectionText);
                        command.ExecuteNonQuery();
                        CustomMessageBox.Show("Main password set successfully.");
                    }
                    else
                    {
                        Application.Current.Shutdown();
                    }
                }
            }
        }

        private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Login_Click(sender, e);
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string mainPassword = MainPasswordBox.Password;
            if (AuthenticateUser(mainPassword))
            {
                CustomMessageBox.Show("Authentication successful!");
                PasswordManagerWindow passwordManagerWindow = new PasswordManagerWindow(mainPassword);
                passwordManagerWindow.Show();
                this.Close();
            }
            else
            {
                CustomMessageBox.Show("Authentication failed!");
            }
        }

        private bool AuthenticateUser(string password)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT EncryptedConnectionText FROM Users WHERE Username = 'admin'";
                var result = command.ExecuteScalar();

                if (result != null)
                {
                    byte[] encryptedText = (byte[])result;
                    string decryptedText = Decrypt(encryptedText, password);

                    return decryptedText == "You are connected";
                }

                return false;
            }
        }

        private string Decrypt(byte[] data, string password)
        {
            try
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
            catch (CryptographicException)
            {
                return null;
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
    }
}
