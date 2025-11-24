using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace IqooNeo10
{
    public partial class SelectedEquipmentWindow : Window
    {
        private readonly int equipmentId;
        private readonly string currentUser;
        private int quantity;
        private int availableQuantity;
        private decimal price;
        private string imagePath;
        private readonly bool openedFromAdmin;
        private bool contactShown = false;

        public SelectedEquipmentWindow(int id, string username, bool showContact = true, bool openedFromAdmin = false, bool hideQuantityField = false)
        {
            InitializeComponent();
            equipmentId = id;
            currentUser = username;
            this.openedFromAdmin = openedFromAdmin;

            ContactButton.Visibility = (showContact && !hideQuantityField) ? Visibility.Visible : Visibility.Collapsed;

            if (hideQuantityField)
            {
                QuantityTextBox.Visibility = Visibility.Collapsed;

                IncreaseQuantityButton.Visibility = Visibility.Collapsed;
                DecreaseQuantityButton.Visibility = Visibility.Collapsed;
                Kolvo.Visibility = Visibility.Collapsed;
            }

            QuantityTextBox.Text = "1";

            DataObject.AddPastingHandler(QuantityTextBox, OnPaste);

            LoadEquipmentInfo();
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();
        }

        private void LoadEquipmentInfo()
        {
            try
            {
                string query = @"SELECT name, model, manufacturer, description, price, quantity, imagepath 
                                 FROM equipment WHERE id = @id";
                var parameters = new Dictionary<string, object> { { "@id", equipmentId } };
                DataTable dt = DatabaseHelper.ExecuteSelect(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Товар больше недоступен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                    return;
                }

                var row = dt.Rows[0];
                string name = row["name"].ToString();
                string model = row["model"].ToString();
                string manuf = row["manufacturer"].ToString();
                string desc = row["description"].ToString();
                price = Convert.ToDecimal(row["price"]);
                quantity = Convert.ToInt32(row["quantity"]);
                availableQuantity = quantity;
                imagePath = row["imagepath"] == DBNull.Value ? null : row["imagepath"].ToString();

                NameTextBlock.Text = name;
                ModelTextBlock.Text = $"Модель: {model}";
                ManufacturerTextBlock.Text = $"Производитель: {manuf}";
                PriceTextBlock.Text = $"Цена: {Convert.ToInt32(price)}";
                QuantityTextBlock.Text = quantity > 0 ? $"В наличии: {quantity}" : "В наличии: Нет в наличии";
                DescriptionTextBlock.Text = string.IsNullOrWhiteSpace(desc) ? "Нет описания." : desc;

                ModelDetailTextBlock.Text = model;
                ManufacturerDetailTextBlock.Text = manuf;

                LoadImage(imagePath);

                ContactPanel.Visibility = contactShown ? Visibility.Visible : Visibility.Collapsed;

                QuantityTextBox.Text = "1";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки информации: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadImage(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path)))
                {
                    EquipmentImage.Source = null;
                    NoImageTextBlock.Visibility = Visibility.Visible;
                    return;
                }

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path), UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                EquipmentImage.Source = bmp;
                NoImageTextBlock.Visibility = Visibility.Collapsed;
            }
            catch
            {
                EquipmentImage.Source = null;
                NoImageTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void ContactButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataTable dt = DatabaseHelper.ExecuteSelect(
                    "SELECT * FROM equipment WHERE id=@id",
                    new Dictionary<string, object> { { "@id", equipmentId } });

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Товара нет в наличии.", "Нет в наличии", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuantityTextBlock.Text = "В наличии: Нет в наличии";
                    return;
                }

                var row = dt.Rows[0];
                int qtyBefore = Convert.ToInt32(row["quantity"]);
                if (qtyBefore <= 0)
                {
                    MessageBox.Show("Товара нет в наличии.", "Нет в наличии", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuantityTextBlock.Text = "В наличии: Нет в наличии";
                    return;
                }

                int qtyToBook = 1;
                if (!int.TryParse(QuantityTextBox.Text, out qtyToBook) || qtyToBook < 1 || qtyToBook > qtyBefore)
                {
                    MessageBox.Show($"Введите корректное количество: от 1 до {qtyBefore}");
                    return;
                }

                string name = row["name"].ToString();
                string model = row["model"].ToString();
                string manufacturer = row["manufacturer"].ToString();
                string description = row["description"].ToString();
                decimal price = Convert.ToDecimal(row["price"]);
                string imagePath = row["imagepath"] == DBNull.Value ? null : row["imagepath"].ToString();

                if (!contactShown)
                {
                    contactShown = true;
                    ContactPanel.Visibility = Visibility.Visible;
                    MessageBox.Show("Данные для связи отображены на карточке товара.", "Контакты", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (MessageBox.Show("Хотите забронировать товар?", "Бронирование", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                DatabaseHelper.ExecuteNonQuery(
                    @"INSERT INTO history
                      (equipment_id, equipment_name, model, manufacturer, description, price, quantity, imagepath, buyer, action)
                      VALUES (@id,@name,@model,@manufacturer,@description,@price,@qty,@image,@buyer,'Забронировано')",
                    new Dictionary<string, object>
                    {
                        {"@id", equipmentId},
                        {"@name", name},
                        {"@model", model},
                        {"@manufacturer", manufacturer},
                        {"@description", description},
                        {"@price", price},
                        {"@qty", qtyToBook},
                        {"@image", imagePath},
                        {"@buyer", currentUser ?? "Гость"}
                    });

                int qtyAfter = qtyBefore - qtyToBook;

                if (qtyAfter > 0)
                {
                    DatabaseHelper.ExecuteNonQuery(
                        "UPDATE equipment SET quantity=@qty WHERE id=@id",
                        new Dictionary<string, object> { { "@qty", qtyAfter }, { "@id", equipmentId } });
                }
                else
                {
                    DatabaseHelper.ExecuteNonQuery(
                        @"INSERT INTO history
                        (equipment_id, equipment_name, model, manufacturer, description, price, quantity, imagepath, seller, action)
                        VALUES (@id,@name,@model,@manufacturer,@description,@price,@qty,@image,@seller,'Товар закончился')",
                        new Dictionary<string, object>
                        {
                        {"@id", equipmentId},
                        {"@name", name},
                        {"@model", model},
                        {"@manufacturer", manufacturer},
                        {"@description", description},
                        {"@price", price},
                        {"@qty", qtyBefore},
                        {"@image", imagePath},
                        {"@seller", currentUser ?? "system"}
                        });

                    DatabaseHelper.ExecuteNonQuery(
                        "UPDATE equipment SET quantity = 0 WHERE id=@id",
                        new Dictionary<string, object> { { "@id", equipmentId } });

                    QuantityTextBlock.Text = "В наличии: Нет в наличии";
                }


                if (qtyAfter > 0)
                    LoadEquipmentInfo();

                MessageBox.Show("Товар успешно забронирован. За подробностями обратитесь к продавцу, по данным для связи", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка бронирования: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы точно хотите покинуть это окно?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
            Close();
        }

        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out int val) || val == 0)
                e.Handled = true;

            string newText = ((System.Windows.Controls.TextBox)sender).Text.Insert(((System.Windows.Controls.TextBox)sender).SelectionStart, e.Text);
            if (!int.TryParse(newText, out int inputVal) || inputVal < 1 || inputVal > availableQuantity)
                e.Handled = true;
        }

        private void QuantityTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(QuantityTextBox.Text))
                QuantityTextBox.Text = "1";
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(QuantityTextBox.Text, out int val) && val < availableQuantity)
                QuantityTextBox.Text = (val + 1).ToString();
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(QuantityTextBox.Text, out int val) && val > 1)
                QuantityTextBox.Text = (val - 1).ToString();
        }
    }
}
