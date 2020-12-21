using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows.Documents;
using System.Windows.Media;

namespace Cabinet
{
    public class DBManager
    {
        private static readonly string DB_FILE = "cabinet.db";
        private static string CONNECTION_URI => string.Format(@"Data Source={0};version=3;", DB_FILE);

        private static DBManager instance = null;
        public static DBManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DBManager();
                }
                return instance;
            }
        }

        private DBManager()
        {
            using (SQLiteConnection connection = new SQLiteConnection(CONNECTION_URI))
            {
                connection.Open();

                SQLiteCommand cmd = new SQLiteCommand(connection)
                {
                    CommandText = @"CREATE TABLE IF NOT EXISTS categories(id INTEGER PRIMARY KEY, name TEXT UNIQUE NOT NULL, iconPath TEXT, color TEXT NOT NULL)"
                };
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS clips(id INTEGER PRIMARY KEY, category_id INTEGER, name TEXT NOT NULL, type INT NOT NULL, content TEXT NOT NULL, FOREIGN KEY (category_id) REFERENCES categories (id))";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "SELECT SQLITE_VERSION();";
                Console.WriteLine(cmd.ExecuteScalar().ToString());

                Console.WriteLine("loaded db");
            }
        }

        public List<Category> GetCategories(MainWindow parentWindow)
        {
            List<Category> categories = new List<Category>();
            using (SQLiteConnection connection = new SQLiteConnection(CONNECTION_URI))
            {
                connection.Open();

                SQLiteCommand cmd = new SQLiteCommand(connection)
                {
                    CommandText = "SELECT * FROM categories"
                };

                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    categories.Add(new Category(parentWindow, reader.GetInt64(0), reader.GetString(1), reader.GetString(2), (Color)ColorConverter.ConvertFromString(reader.GetString(3))));
                }
            }
            return categories;
        }

        public long AddCategory(string name, string icon, Color color)
        {
            using (SQLiteConnection connection = new SQLiteConnection(CONNECTION_URI))
            {
                connection.Open();

                SQLiteCommand cmd = new SQLiteCommand(connection)
                {
                    CommandText = string.Format(@"INSERT INTO categories(name, iconPath, color) VALUES ('{0}', '{1}', '{2}')", name, icon, color.ToString())
                };
                cmd.ExecuteNonQuery();

                Console.WriteLine("category added");
                return connection.LastInsertRowId;
            }
        }

        public void UpdateCategory(long id, string name, string icon, Color color)
        {
            using (SQLiteConnection connection = new SQLiteConnection(CONNECTION_URI))
            {
                connection.Open();

                SQLiteCommand cmd = new SQLiteCommand(connection)
                {
                    CommandText = string.Format(@"UPDATE categories SET name=@name, iconPath=@iconPath, color=@color WHERE id=@id")
                };
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@iconPath", icon);
                cmd.Parameters.AddWithValue("@color", color.ToString());
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                Console.WriteLine("category updated");
            }
        }

        public List<ClipboardObject> GetClipboardObjects(MainWindow parentWindow, long categoryId)
        {
            List<ClipboardObject> clipboardObjects = new List<ClipboardObject>();
            using (SQLiteConnection connection = new SQLiteConnection(CONNECTION_URI))
            {
                connection.Open();

                SQLiteCommand cmd = new SQLiteCommand(connection)
                {
                    CommandText = "SELECT * FROM clips where category_id=@category_id ORDER BY name COLLATE NOCASE"
                };
                cmd.Parameters.AddWithValue("@category_id", categoryId);
                cmd.Prepare();

                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    clipboardObjects.Add(ClipboardObjectUtils.CreateClipboardObjectByType(parentWindow, reader.GetInt64(0), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
                }
            }
            return clipboardObjects;
        }

        public long AddClipboardObject(long categoryId, string name, string type, string content)
        {
            using (SQLiteConnection connection = new SQLiteConnection(CONNECTION_URI))
            {
                connection.Open();

                SQLiteCommand cmd = new SQLiteCommand(connection)
                {
                    CommandText = string.Format(@"INSERT INTO clips(category_id, name, type, content) VALUES ({0}, '{1}', '{2}', '{3}')", categoryId, name, type, content)
                };
                cmd.ExecuteNonQuery();

                Console.WriteLine("clip added");
                return connection.LastInsertRowId;
            }
        }

        public void DeleteClipboardObject(long id)
        {
            using (SQLiteConnection connection = new SQLiteConnection(CONNECTION_URI))
            {
                connection.Open();

                SQLiteCommand cmd = new SQLiteCommand(connection)
                {
                    CommandText = string.Format(@"DELETE FROM clips WHERE id=@id")
                };
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                Console.WriteLine("clipboard object deleted");
            }
        }
    }
}
