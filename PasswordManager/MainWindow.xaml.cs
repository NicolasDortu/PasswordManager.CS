using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using Microsoft.VisualBasic;

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
            if (!File.Exists("passwords.db"))
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
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
                            EncryptedPassword BLOB NOT NULL,
                            FOREIGN KEY(UserID) REFERENCES Users(UserID)
                        );

                        INSERT INTO Users (Username, EncryptedConnectionText) VALUES ('admin', @encryptedConnectionText);
                    ";

                    string connectionText = "You are connected";
                    string defaultPassword = "admin"; // Change this to set your default main password
                    byte[] encryptedConnectionText = Encrypt(connectionText, defaultPassword);

                    command.Parameters.AddWithValue("@encryptedConnectionText", encryptedConnectionText);
                    command.ExecuteNonQuery();
                }

                MessageBox.Show("Database created and initialized. Please use the default password 'admin' to login.");
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string mainPassword = MainPasswordBox.Password;
            if (AuthenticateUser(mainPassword))
            {
                MessageBox.Show("Authentication successful!");
                OptionsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Authentication failed!");
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

        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            string mainPassword = MainPasswordBox.Password;
            string appName = Interaction.InputBox("Enter the app name:", "App Name", "Default");
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
            string mainPassword = MainPasswordBox.Password;
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
    }
}
