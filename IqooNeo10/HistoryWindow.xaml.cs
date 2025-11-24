using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IqooNeo10
{
    public partial class HistoryWindow : Window
    {
        private readonly string currentUser;
        private readonly bool openedFromAdmin;

        private DataView historyView;
        private DataTable historyTable; 

        public HistoryWindow(string username, bool fromAdmin = false)
        {
            InitializeComponent();
            currentUser = username ?? string.Empty;
            openedFromAdmin = fromAdmin;

            DataObject.AddPastingHandler(MinPriceTextBox, BlockPaste);
            DataObject.AddPastingHandler(MaxPriceTextBox, BlockPaste);

            LoadHistory();
        }

        private void LoadHistory()
        {
            try
            {
                string query = @"SELECT 
                                id AS 'ID',
                                equipment_name AS 'Название',
                                model AS 'Модель',
                                manufacturer AS 'Производитель',
                                price AS 'Цена',
                                quantity AS 'Количество',
                                seller AS 'Продавец',
                                buyer AS 'Покупатель',
                                action AS 'Действие',
                                created_at AS 'Дата'
                                FROM history
                                ORDER BY created_at DESC";

                historyTable = DatabaseHelper.ExecuteSelect(query);
                historyView = historyTable.DefaultView;

                HistoryGrid.ItemsSource = historyView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки истории: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы точно хотите покинуть это окно?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                if (openedFromAdmin)
                    new AdminWindow(currentUser, true).Show();
                else
                    new MainWindow().Show();
            }
            catch
            {
                new MainWindow().Show();
            }

            this.Close();
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (historyView == null) return;

                List<string> filters = new List<string>();

                if (!string.IsNullOrWhiteSpace(NameFilterTextBox.Text))
                    filters.Add($"[Название] LIKE '%{NameFilterTextBox.Text.Trim()}%'");

                if (!string.IsNullOrWhiteSpace(ManufacturerFilterTextBox.Text))
                    filters.Add($"[Производитель] LIKE '%{ManufacturerFilterTextBox.Text.Trim()}%'");

                if (int.TryParse(MinPriceTextBox.Text, out int minPrice))
                    filters.Add($"[Цена] >= {minPrice}");

                if (int.TryParse(MaxPriceTextBox.Text, out int maxPrice))
                    filters.Add($"[Цена] <= {maxPrice}");

                if (ActionFilterComboBox.SelectedItem is ComboBoxItem selectedItem && !string.IsNullOrEmpty(selectedItem.Content.ToString()))
                    filters.Add($"[Действие] = '{selectedItem.Content}'");

                historyView.RowFilter = string.Join(" AND ", filters);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка фильтрации: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            NameFilterTextBox.Clear();
            ManufacturerFilterTextBox.Clear();
            MinPriceTextBox.Clear();
            MaxPriceTextBox.Clear();
            ActionFilterComboBox.SelectedIndex = 0;

            if (historyView != null)
            {
                historyView.RowFilter = string.Empty;
            }

            HistoryGrid.Items.SortDescriptions.Clear();
            foreach (var column in HistoryGrid.Columns)
                column.SortDirection = null;
        }

        private void Price_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var box = sender as TextBox;
            string newText = box.Text.Insert(box.SelectionStart, e.Text);

            if (!e.Text.All(char.IsDigit)) { e.Handled = true; return; }
            if (newText.Length > 1 && newText.StartsWith("0")) { e.Handled = true; return; }
            if (int.TryParse(newText, out int val) && val > 250000) { e.Handled = true; return; }

            e.Handled = false;
        }

        private void BlockPaste(object sender, DataObjectPastingEventArgs e) => e.CancelCommand();
    }
}
