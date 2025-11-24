using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace IqooNeo10
{
    /// <summary>
    /// Класс MainWindow реализует окно авторизации пользователей для роли Продавец/Администратор.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly bool isSellerOrAdmin;

        public MainWindow(bool isSellerOrAdmin = false)
        {
            InitializeComponent();
            this.isSellerOrAdmin = isSellerOrAdmin;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "";

            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                StatusTextBlock.Text = "Введите логин и пароль.";
                return;
            }

            try
            {
                string query = "SELECT password, role FROM users WHERE username=@u";
                var parameters = new Dictionary<string, object> { { "@u", username } };
                DataTable dt = DatabaseHelper.ExecuteSelect(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    StatusTextBlock.Text = "Пользователь не найден.";
                    return;
                }

                string storedHash = dt.Rows[0]["password"].ToString();
                string role = dt.Rows[0]["role"].ToString();

                if (!PasswordHelper.VerifyPassword(password, storedHash))
                {
                    StatusTextBlock.Text = "Неверный пароль.";
                    return;
                }

                Window nextWindow = null;

                switch (role)
                {
                    case "admin":
                        nextWindow = new AdminWindow(username, true);
                        break;

                    case "seller":
                        nextWindow = new MarketWindow(username, canEdit: true, showContact: false, openedFromAdmin: false, isAdminView: false);
                        break;

                    default:
                        StatusTextBlock.Text = "Неизвестная роль пользователя.";
                        return;
                }

                nextWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при попытке входа: " + ex.Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы точно хотите покинуть это окно?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
            new RoleSelectionWindow().Show();
            this.Close();
        }
    }
}
