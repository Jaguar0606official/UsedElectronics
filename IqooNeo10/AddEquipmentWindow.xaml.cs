using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace IqooNeo10
{
    /// <summary>
    /// Класс <see cref="AddEquipmentWindow"/> описывает логику окна добавления нового оборудования.
    /// Содержит методы для валидации вводимых данных, загрузки изображений и записи информации в базу данных.
    /// </summary>
    public partial class AddEquipmentWindow : Window
    {
        private readonly string currentUser;
        private string imagePath;
        private readonly string imageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");

        /// <summary>
        /// Конструктор окна добавления оборудования.
        /// Инициализирует интерфейс и создаёт папку для хранения изображений, если она отсутствует.
        /// </summary>
        /// <param name="user">Имя текущего пользователя (продавца).</param>
        public AddEquipmentWindow(string user)
        {
            InitializeComponent();
            currentUser = user;
            Directory.CreateDirectory(imageDir);
        }

        /// <summary>
        /// Запрещает вставку текста (Ctrl+V, контекстное меню).
        /// Используется для полей, где разрешён только ручной ввод.
        /// </summary>
        private void BlockPaste(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();
        }

        /// <summary>
        /// Открывает диалог выбора изображения и сохраняет выбранное фото в папку проекта.
        /// После выбора отображает предпросмотр изображения в окне.
        /// </summary>
        private void ChooseImage_Click(object s, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" };
            if (dlg.ShowDialog() != true) return;

            string file = DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(dlg.FileName);
            File.Copy(dlg.FileName, Path.Combine(imageDir, file), true);

            imagePath = "images/" + file;
            ImagePathLabel.Text = imagePath;
            PreviewImage.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath)));
        }

        /// <summary>
        /// Выполняет сохранение информации о новом оборудовании в базу данных.
        /// Проводит проверку корректности введённых данных перед записью.
        /// После успешного добавления создаёт запись в таблице истории.
        /// </summary>
        private void Save_Click(object s, RoutedEventArgs e)
        {
            if (new[] { NameTextBox, ModelTextBox, ManufacturerTextBox, DescriptionTextBox, PriceTextBox, QuantityTextBox }
                .Any(x => string.IsNullOrWhiteSpace(x.Text)))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PriceTextBox.Text, out int price) || price < 1 || price > 250000)
            {
                MessageBox.Show("Цена от 1 до 250000", "Ошибка");
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty < 1 || qty > 64)
            {
                MessageBox.Show("Количество от 1 до 64", "Ошибка");
                return;
            }

            var p = new Dictionary<string, object>
            {
                {"@n", NameTextBox.Text.Trim()},
                {"@m", ModelTextBox.Text.Trim()},
                {"@man", ManufacturerTextBox.Text.Trim()},
                {"@d", DescriptionTextBox.Text.Trim()},
                {"@p", Convert.ToDecimal(price)},
                {"@q", qty},
                {"@img", string.IsNullOrWhiteSpace(imagePath) ? (object)DBNull.Value : imagePath }
            };

            DatabaseHelper.ExecuteNonQuery(
                "INSERT INTO equipment (name,model,manufacturer,description,price,quantity,imagepath) VALUES (@n,@m,@man,@d,@p,@q,@img)", p);

            int id = Convert.ToInt32(DatabaseHelper.ExecuteSelect("SELECT LAST_INSERT_ID()").Rows[0][0]);

            DatabaseHelper.ExecuteNonQuery(
                @"INSERT INTO history (equipment_id,equipment_name,model,manufacturer,description,price,quantity,imagepath,seller,action)
                  VALUES (@id,@n,@m,@man,@d,@p,@q,@img,@s,'Добавлено')",
                new Dictionary<string, object>(p) { { "@id", id }, { "@s", currentUser } });

            MessageBox.Show("Добавлено!", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        /// <summary>
        /// Закрывает текущее окно без сохранения данных.
        /// </summary>
        private void Cancel_Click(object s, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы точно хотите покинуть это окно?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
            Close();
        }
       

        /// <summary>
        /// Разрешает ввод только цифр в текстовое поле.
        /// </summary>
        private void OnlyNum(object s, System.Windows.Input.TextCompositionEventArgs e) => e.Handled = !char.IsDigit(e.Text, 0);

        /// <summary>
        /// Проверяет корректность вводимой цены.
        /// Запрещает ввод символов, ведущих нулей и чисел больше 250000.
        /// </summary>
        private void Price_PreviewTextInput(object s, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (IsLeadingZero((TextBox)s, e.Text)) { e.Handled = true; return; }

            OnlyNum(s, e);
            if (int.TryParse(((TextBox)s).Text + e.Text, out int v)) e.Handled = v > 250000;
        }

        /// <summary>
        /// Проверяет корректность вводимого количества.
        /// Запрещает ввод символов, ведущих нулей и чисел больше 64.
        /// </summary>
        private void Quantity_PreviewTextInput(object s, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (IsLeadingZero((TextBox)s, e.Text)) { e.Handled = true; return; }

            OnlyNum(s, e);
            if (int.TryParse(((TextBox)s).Text + e.Text, out int v)) e.Handled = v > 64;
        }

        /// <summary>
        /// Проверяет, начинается ли вводимое значение с нуля.
        /// </summary>
        /// <param name="tb">Текстовое поле, в которое производится ввод.</param>
        /// <param name="input">Вводимый символ.</param>
        /// <returns>True, если строка начинается с нуля; иначе False.</returns>
        private bool IsLeadingZero(TextBox tb, string input)
        {
            string newText = tb.Text.Insert(tb.CaretIndex, input);
            return newText.StartsWith("0");
        }

    }
}
