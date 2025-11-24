using System;
using System.Windows;

namespace IqooNeo10
{
    public partial class AdminWindow : Window
    {
        private readonly string currentUser;

        public AdminWindow(string username, bool fromSellerOrChild = false)
        {
            InitializeComponent();
            currentUser = username;
        }

        private void BackToAvtorization_Button_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы точно хотите покинуть это окно?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
            new MainWindow().Show();
            Close();
        }

        private void OpenMarket_Button_Click(object sender, RoutedEventArgs e)
        {
            var win = new MarketWindow(
                username: currentUser,
                canEdit: false,         
                showContact: false,     
                openedFromAdmin: true,
                isAdminView: true       
            );

            win.Show();
            this.Close();
        }


        private void OpenHistory_Button_Click(object sender, RoutedEventArgs e)
        {
            new HistoryWindow(currentUser, true).Show();
            Close();
        }

        private void ClearAllData_Button_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Удалить все данные об электронике и истории?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                DatabaseHelper.ExecuteNonQuery("DELETE FROM history");
                DatabaseHelper.ExecuteNonQuery("DELETE FROM equipment");
                MessageBox.Show("Данные успешно удалены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при очистке данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
