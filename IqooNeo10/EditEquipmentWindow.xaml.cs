using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace IqooNeo10
{
    /// <summary>
    /// Класс <c>EditEquipmentWindow</c> описывает логику окна редактирования оборудования.
    /// Содержит методы для загрузки данных, изменения информации о товаре и валидации вводимых значений.
    /// </summary>
    public partial class EditEquipmentWindow : Window
    {
        private readonly int id;
        private readonly string user;
        private string imagePath;
        private readonly string imgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");

        /// <summary>
        /// Конструктор окна редактирования оборудования.
        /// Инициализирует компоненты, создает каталог изображений (если отсутствует),
        /// загружает данные выбранного товара и блокирует возможность вставки текста в числовые поля.
        /// </summary>
        public EditEquipmentWindow(int equipmentId, string username)
        {
            InitializeComponent();
            id = equipmentId;
            user = username;
            Directory.CreateDirectory(imgDir);
            LoadData();
            DataObject.AddPastingHandler(PriceTextBox, BlockPaste);
            DataObject.AddPastingHandler(QuantityTextBox, BlockPaste);
        }

        /// <summary>
        /// Метод загружает данные выбранного оборудования из базы данных
        /// и отображает их в соответствующих полях окна.
        /// </summary>
        private void LoadData()
        {
            var dt = DatabaseHelper.ExecuteSelect(
                "SELECT * FROM equipment WHERE id=@id",
                new Dictionary<string, object> { { "@id", id } });

            if (dt.Rows.Count == 0) { MessageBox.Show("Товар не найден!"); Close(); return; }

            var r = dt.Rows[0];
            NameTextBox.Text = r["name"].ToString();
            ModelTextBox.Text = r["model"].ToString();
            ManufacturerTextBox.Text = r["manufacturer"].ToString();
            DescriptionTextBox.Text = r["description"].ToString();
            PriceTextBox.Text = Convert.ToInt32(Convert.ToDecimal(r["price"])).ToString();
            QuantityTextBox.Text = r["quantity"].ToString();

            imagePath = r["imagepath"] == DBNull.Value ? null : r["imagepath"].ToString();
            ImagePathLabel.Text = imagePath ?? "Фото не выбрано";
            PreviewImage.Source = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath ?? ""))
                ? new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath)))
                : null;
        }

        /// <summary>
        /// Обработчик нажатия кнопки выбора изображения.
        /// Позволяет пользователю выбрать изображение и копирует его в папку проекта.
        /// </summary>
        private void ChooseImage_Click(object s, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" };
            if (d.ShowDialog() != true) return;

            string file = DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(d.FileName);
            File.Copy(d.FileName, Path.Combine(imgDir, file), true);
            imagePath = "images/" + file;

            ImagePathLabel.Text = imagePath;
            PreviewImage.Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath)));
        }

        /// <summary>
        /// Очищает выбранное изображение, сбрасывая путь и предпросмотр.
        /// </summary>
        private void ClearImage_Click(object s, RoutedEventArgs e)
        {
            imagePath = null;
            ImagePathLabel.Text = "Фото не выбрано";
            PreviewImage.Source = null;
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Сохранить".
        /// Выполняет проверку введённых данных, обновляет запись в таблице <c>equipment</c>
        /// и добавляет информацию в историю изменений.
        /// </summary>
        private void Save_Click(object s, RoutedEventArgs e)
        {
            if (new[] { NameTextBox, ModelTextBox, ManufacturerTextBox, DescriptionTextBox, PriceTextBox, QuantityTextBox }
                .Any(x => string.IsNullOrWhiteSpace(x.Text)))
            {
                MessageBox.Show("Заполните все поля!"); return;
            }

            if (!int.TryParse(PriceTextBox.Text, out int price) || price < 1 || price > 250000)
            {
                MessageBox.Show("Цена от 1 до 250000"); return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty < 1 || qty > 64)
            {
                MessageBox.Show("Количество от 1 до 64"); return;
            }

            var p = new Dictionary<string, object>
            {
                {"@id", id},
                {"@n", NameTextBox.Text.Trim()},
                {"@m", ModelTextBox.Text.Trim()},
                {"@man", ManufacturerTextBox.Text.Trim()},
                {"@d", DescriptionTextBox.Text.Trim()},
                {"@p", Convert.ToDecimal(price)},
                {"@q", qty},
                {"@img", string.IsNullOrEmpty(imagePath) ? (object)DBNull.Value : imagePath }
            };

            DatabaseHelper.ExecuteNonQuery(
                @"UPDATE equipment SET name=@n,model=@m,manufacturer=@man,description=@d,
                  price=@p,quantity=@q,imagepath=@img WHERE id=@id", p);

            DatabaseHelper.ExecuteNonQuery(
                @"INSERT INTO history (equipment_id,equipment_name,model,manufacturer,description,price,quantity,imagepath,seller,action)
                  VALUES (@id,@n,@m,@man,@d,@p,@q,@img,@s,'Изменено')",
                new Dictionary<string, object>(p) { { "@s", user } });

            MessageBox.Show("Сохранено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        /// <summary>
        /// Проверяет, чтобы вводимые символы в текстовые поля были только цифрами.
        /// </summary>
        private void OnlyNum(object s, System.Windows.Input.TextCompositionEventArgs e) =>
            e.Handled = !char.IsDigit(e.Text, 0);

        /// <summary>
        /// Ограничивает ввод цены: только числа, без нулей в начале и не более 250000.
        /// </summary>
        private void Price_PreviewTextInput(object s, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (IsLeadingZero((TextBox)s, e.Text)) { e.Handled = true; return; }

            OnlyNum(s, e);
            if (int.TryParse(((TextBox)s).Text + e.Text, out int v)) e.Handled = v > 250000;
        }

        /// <summary>
        /// Ограничивает ввод количества: только числа, без нулей в начале и не более 64.
        /// </summary>
        private void Quantity_PreviewTextInput(object s, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (IsLeadingZero((TextBox)s, e.Text)) { e.Handled = true; return; }

            OnlyNum(s, e);
            if (int.TryParse(((TextBox)s).Text + e.Text, out int v)) e.Handled = v > 64;
        }

        /// <summary>
        /// Закрывает текущее окно без сохранения изменений.
        /// </summary>
        private void Cancel_Click(object s, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы точно хотите покинуть это окно?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
            Close();
        }

        /// <summary>
        /// Блокирует вставку текста (Ctrl+V, контекстное меню) в числовые поля.
        /// </summary>
        private void BlockPaste(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();
        }

        /// <summary>
        /// Проверяет, начинается ли вводимое значение с нуля.
        /// Используется для предотвращения некорректного ввода.
        /// </summary>
        private bool IsLeadingZero(TextBox tb, string input)
        {
            string newText = tb.Text.Insert(tb.CaretIndex, input);
            return newText.StartsWith("0");
        }
    }
}
