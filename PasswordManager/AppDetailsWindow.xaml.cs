using System.Text;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace PasswordManagerWPF
{
    public partial class AppDetailsWindow : Window
    {
        private int passwordId;
        private string mainPassword;
        private string connectionString;

        public AppDetailsWindow(int passwordId, string mainPassword, string connectionString)
        {
            InitializeComponent();
            this.passwordId = passwordId;
            this.mainPassword = mainPassword;
            this.connectionString = connectionString;
            LoadAppDetails();
        }

        private void LoadAppDetails()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT AppName, EncryptedPassword, Login FROM Passwords WHERE PasswordID = @passwordId";
                command.Parameters.AddWithValue("@passwordId", passwordId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        AppNameTextBox.Text = reader.GetString(reader.GetOrdinal("AppName"));

                        byte[] encryptedPassword = (byte[])reader["EncryptedPassword"];
                        string decryptedPassword = Decrypt(encryptedPassword, mainPassword);
                        PasswordTextBox.Text = decryptedPassword;

                        if (!reader.IsDBNull(reader.GetOrdinal("Login")))
                        {
                            LoginTextBox.Text = reader.GetString(reader.GetOrdinal("Login"));
                        }
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string newAppName = AppNameTextBox.Text;
            string login = LoginTextBox.Text;
            string newPassword = PasswordTextBox.Text;
            byte[] encryptedPassword = Encrypt(newPassword, mainPassword);

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Passwords 
                    SET AppName = @newAppName, Login = @login, EncryptedPassword = @encryptedPassword 
                    WHERE PasswordID = @passwordId";
                command.Parameters.AddWithValue("@newAppName", newAppName);
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@encryptedPassword", encryptedPassword);
                command.Parameters.AddWithValue("@passwordId", passwordId);
                command.ExecuteNonQuery();
            }

            MessageBox.Show("Information saved successfully.");
            this.Close();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to delete this entry?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM Passwords WHERE PasswordID = @passwordId";
                    command.Parameters.AddWithValue("@passwordId", passwordId);
                    command.ExecuteNonQuery();
                }

                MessageBox.Show("Entry deleted successfully.");
                this.Close();
            }
        }

        private string Decrypt(byte[] data, string password)
        {
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                var key = new System.Security.Cryptography.Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("SaltIsGoodForYou")).GetBytes(32);
                aes.Key = key;
                aes.IV = new byte[16];

                using (var decryptor = aes.CreateDecryptor())
                {
                    var result = decryptor.TransformFinalBlock(data, 0, data.Length);
                    return Encoding.UTF8.GetString(result);
                }
            }
        }

        private byte[] Encrypt(string text, string password)
        {
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                var key = new System.Security.Cryptography.Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("SaltIsGoodForYou")).GetBytes(32);
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
