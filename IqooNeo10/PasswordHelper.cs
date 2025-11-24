using System;
using System.Security.Cryptography;
using System.Text;

namespace IqooNeo10
{
    /// <summary>
    /// Статический класс <c>PasswordHelper</c> реализует функционал
    /// хеширования и проверки паролей пользователей.
    /// Используется для безопасного хранения паролей в базе данных
    /// и их последующей верификации при входе в систему.
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Выполняет хеширование введённого пароля с помощью алгоритма SHA-256.
        /// Возвращает строку с шестнадцатеричным представлением хеша.
        /// </summary>
        /// <param name="password">Исходный пароль пользователя.</param>
        /// <returns>Хеш пароля в виде строки (строчные символы, без дефисов).</returns>
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Преобразование текста пароля в байты
                byte[] bytes = Encoding.UTF8.GetBytes(password);

                // Вычисление хеша SHA-256
                byte[] hash = sha256.ComputeHash(bytes);

                // Преобразование результата в строку без разделителей
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Проверяет, совпадает ли введённый пользователем пароль
        /// с хешем, хранящимся в базе данных.
        /// </summary>
        /// <param name="enteredPassword">Пароль, введённый пользователем.</param>
        /// <param name="storedHash">Хеш пароля, сохранённый в базе данных.</param>
        /// <returns><see langword="true"/> — если пароли совпадают; иначе <see langword="false"/>.</returns>
        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            // Хешируем введённый пароль
            string hashOfInput = HashPassword(enteredPassword);

            // Сравниваем хеши без учёта регистра
            return hashOfInput == storedHash;
        }
    }
}
