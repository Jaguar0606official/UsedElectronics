using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace IqooNeo10
{
    /// <summary>
    /// Статический класс <see cref="DatabaseHelper"/> реализует вспомогательные методы
    /// для взаимодействия приложения с базой данных MySQL.
    /// </summary>
    /// <remarks>
    /// В классе предусмотрены универсальные методы для выполнения SQL-запросов:
    /// - выборка данных (SELECT);
    /// - получение скалярных значений (например, COUNT или LAST_INSERT_ID);
    /// - выполнение команд без возврата данных (INSERT, UPDATE, DELETE).
    /// Также реализовано безопасное добавление параметров для предотвращения SQL-инъекций.
    /// </remarks>
    public static class DatabaseHelper
    {
        /// <summary>
        /// Строка подключения к базе данных MySQL.
        /// Загружается из конфигурационного файла <c>App.config</c> (раздел <c>connectionStrings</c>).
        /// </summary>
        private static readonly string connectionString =
            ConfigurationManager.ConnectionStrings["MyConnection"].ConnectionString;

        /// <summary>
        /// Создаёт и возвращает новый объект подключения к базе данных.
        /// </summary>
        /// <returns><see cref="MySqlConnection"/> — открываемое подключение к БД.</returns>
        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }

        /// <summary>
        /// Выполняет SQL-запрос типа <c>SELECT</c> и возвращает результат в виде таблицы <see cref="DataTable"/>.
        /// </summary>
        /// <param name="query">SQL-запрос.</param>
        /// <param name="parameters">Словарь параметров для подстановки в запрос.</param>
        /// <returns>Результат выполнения запроса в виде <see cref="DataTable"/>.</returns>
        public static DataTable ExecuteSelect(string query, Dictionary<string, object> parameters = null)
        {
            var dt = new DataTable();

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                {
                    AddParameters(cmd, parameters);

                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                }
            }

            return dt;
        }

        /// <summary>
        /// Выполняет SQL-запрос и возвращает одно скалярное значение.
        /// </summary>
        /// <param name="query">SQL-запрос (например, <c>SELECT COUNT(*)</c>).</param>
        /// <param name="parameters">Параметры запроса.</param>
        /// <returns>Первое значение из первой строки результата запроса.</returns>
        public static object ExecuteScalar(string query, Dictionary<string, object> parameters = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                {
                    AddParameters(cmd, parameters);
                    return cmd.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Выполняет SQL-команду, не возвращающую данные (например, INSERT, UPDATE, DELETE).
        /// </summary>
        /// <param name="query">Текст SQL-запроса.</param>
        /// <param name="parameters">Параметры для команды (опционально).</param>
        /// <returns>Количество затронутых строк.</returns>
        public static int ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                {
                    AddParameters(cmd, parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Добавляет параметры в SQL-команду, предотвращая SQL-инъекции.
        /// Автоматически определяет тип данных (int, decimal, string и т. д.)
        /// и корректно устанавливает Precision и Scale для числовых значений.
        /// </summary>
        /// <param name="cmd">Объект команды <see cref="MySqlCommand"/>.</param>
        /// <param name="parameters">Словарь параметров для добавления.</param>
        private static void AddParameters(MySqlCommand cmd, Dictionary<string, object> parameters)
        {
            if (parameters == null) return;

            foreach (var kv in parameters)
            {
                string name = kv.Key.StartsWith("@") ? kv.Key : "@" + kv.Key;
                object val = kv.Value ?? DBNull.Value;

                if (val is int i)
                {
                    cmd.Parameters.Add(name, MySqlDbType.Int32).Value = i;
                }
                else if (val is decimal dec)
                {
                    var p = new MySqlParameter(name, MySqlDbType.Decimal)
                    {
                        Precision = 10,
                        Scale = 2,
                        Value = dec
                    };
                    cmd.Parameters.Add(p);
                }
                else if (val is double db)
                {
                    var p = new MySqlParameter(name, MySqlDbType.Decimal)
                    {
                        Precision = 10,
                        Scale = 2,
                        Value = Convert.ToDecimal(db)
                    };
                    cmd.Parameters.Add(p);
                }
                else
                {
                    cmd.Parameters.Add(name, MySqlDbType.VarChar).Value = val;
                }
            }
        }
    }
}
