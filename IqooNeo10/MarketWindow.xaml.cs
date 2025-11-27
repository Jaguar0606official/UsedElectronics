using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IqooNeo10
{
    public partial class MarketWindow : Window
    {
        private readonly string currentUser;
        private readonly bool canEdit;          // продавец
        private readonly bool showContact;
        private readonly bool openedFromAdmin;
        private readonly bool isAdminView;      // администратор

        public MarketWindow(
            string username = null,
            bool canEdit = false,
            bool showContact = true,
            bool openedFromAdmin = false,
            bool isAdminView = false)
        {
            InitializeComponent();

            currentUser = username;
            this.canEdit = canEdit;
            this.showContact = showContact;
            this.openedFromAdmin = openedFromAdmin;
            this.isAdminView = isAdminView;

            //
            // Управление кнопками
            //
            if (isAdminView || !canEdit)
            {
                AddButton.Visibility = Visibility.Collapsed;
                EditButton.Visibility = Visibility.Collapsed;
                DeleteButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                AddButton.Visibility = Visibility.Visible;
                EditButton.Visibility = Visibility.Visible;
                DeleteButton.Visibility = Visibility.Visible;
            }

            LoadEquipment();
        }

        // ===============================================
        // ЗАГРУЗКА СПИСКА ТОВАРОВ С УЧЁТОМ РОЛИ
        // ===============================================
        private void LoadEquipment()
        {
            string q;

            if (!canEdit && !isAdminView)
            {
                // Покупатель → не показываем товары с quantity = 0
                q = @"
                    SELECT
                        id AS 'ID',
                        name AS 'Название',
                        model AS 'Модель',
                        manufacturer AS 'Производитель',
                        price AS 'Цена',
                        quantity AS 'Количество'
                    FROM equipment
                    WHERE quantity > 0";
            }
            else
            {
                // Продавец или админ → показываем всё
                q = @"
                    SELECT
                        id AS 'ID',
                        name AS 'Название',
                        model AS 'Модель',
                        manufacturer AS 'Производитель',
                        price AS 'Цена',
                        quantity AS 'Количество'
                    FROM equipment";
            }

            try
            {
                EquipmentGrid.ItemsSource = DatabaseHelper
                    .ExecuteSelect(q)
                    .DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        private int? GetSelectedId()
        {
            if (EquipmentGrid.SelectedItem is DataRowView r)
                return Convert.ToInt32(r["ID"]);

            MessageBox.Show("Выберите товар для редактирования");
            return null;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            new AddEquipmentWindow(currentUser).ShowDialog();
            LoadEquipment();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedId();
            if (id == null) return;

            new EditEquipmentWindow(id.Value, currentUser).ShowDialog();
            LoadEquipment();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedId();
            if (id == null) return;

            if (MessageBox.Show("Убрать товар из продажи?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            DatabaseHelper.ExecuteNonQuery(
                @"INSERT INTO history (equipment_id, equipment_name, model, manufacturer, description, price, quantity, imagepath, seller, action)
          SELECT id, name, model, manufacturer, description, price, quantity, imagepath, @user, 'Удалено'
          FROM equipment WHERE id=@id",
                new Dictionary<string, object> {
            { "@id", id },
            { "@user", currentUser ?? "system" }
                });

            DatabaseHelper.ExecuteNonQuery("DELETE FROM equipment WHERE id=@id", new Dictionary<string, object> { { "@id", id } });

            LoadEquipment();
        }


        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы точно хотите покинуть это окно?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            if (isAdminView)
            {
                new AdminWindow(currentUser, true).Show();
            }
            else if (canEdit)
            {
                new MainWindow().Show();
            }
            else
            {
                new RoleSelectionWindow().Show();
            }

            Close();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            int minPrice = string.IsNullOrWhiteSpace(MinPriceTextBox.Text) ? -1 : int.Parse(MinPriceTextBox.Text);
            int maxPrice = string.IsNullOrWhiteSpace(MaxPriceTextBox.Text) ? -1 : int.Parse(MaxPriceTextBox.Text);

            string q = @"
                SELECT
                    id AS 'ID',
                    name AS 'Название',
                    model AS 'Модель',
                    manufacturer AS 'Производитель',
                    price AS 'Цена',
                    quantity AS 'Количество'
                FROM equipment
                WHERE (@n = '' OR name LIKE CONCAT('%', @n, '%'))
                AND (@m = '' OR manufacturer LIKE CONCAT('%', @m, '%'))
                AND (@min = -1 OR price >= @min)
                AND (@max = -1 OR price <= @max)";

            // Покупатель — исключаем товары с quantity = 0
            if (!canEdit && !isAdminView)
            {
                q += " AND quantity > 0";
            }

            var pars = new Dictionary<string, object>
            {
                { "@n", NameFilterTextBox.Text.Trim() },
                { "@m", ManufacturerFilterTextBox.Text.Trim() },
                { "@min", minPrice },
                { "@max", maxPrice }
            };

            EquipmentGrid.ItemsSource = DatabaseHelper.ExecuteSelect(q, pars).DefaultView;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            NameFilterTextBox.Clear();
            ManufacturerFilterTextBox.Clear();
            MinPriceTextBox.Clear();
            MaxPriceTextBox.Clear();
            LoadEquipment();
        }

        private void EquipmentGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(EquipmentGrid.SelectedItem is DataRowView r)) return;
            int id = Convert.ToInt32(r["ID"]);

            bool hideQuantityAndContact = isAdminView || canEdit;

            var win = new SelectedEquipmentWindow(
                id,
                currentUser,
                showContact: showContact,
                openedFromAdmin: openedFromAdmin,
                hideQuantityField: hideQuantityAndContact
            );

            win.ShowDialog();
            LoadEquipment();
        }

        private void Price_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var box = (TextBox)sender;
            string newText = box.Text.Insert(box.SelectionStart, e.Text);

            if (!e.Text.All(char.IsDigit)) { e.Handled = true; return; }
            if (newText.Length > 1 && newText.StartsWith("0")) { e.Handled = true; return; }
            if (int.TryParse(newText, out int v) && v > 250000) { e.Handled = true; return; }
            e.Handled = false;
        }

        private void BlockPaste(object sender, DataObjectPastingEventArgs e) => e.CancelCommand();
    }
}
