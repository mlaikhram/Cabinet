using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows.Media;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
                    CommandText = @"CREATE TABLE IF NOT EXISTS categories(id INTEGER PRIMARY KEY, name TEXT UNIQUE NOT NULL, icon TEXT, color TEXT NOT NULL)"
                };
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS clips(id INTEGER PRIMARY KEY, category_id INTEGER, name TEXT NOT NULL, type INT NOT NULL, content TEXT NOT NULL, FOREIGN KEY (category_id) REFERENCES categories (id))";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "SELECT SQLITE_VERSION();";
                Console.WriteLine(cmd.ExecuteScalar().ToString());

                Console.WriteLine("created db");
            }
        }

        public void AddCategory(string name, string icon, Color color)
        {
            using (SQLiteConnection connection = new SQLiteConnection(CONNECTION_URI))
            {
                connection.Open();

                SQLiteCommand cmd = new SQLiteCommand(connection)
                {
                    CommandText = string.Format(@"INSERT INTO categories(name, icon, color) VALUES ('{0}', '{1}', '{2}')", name, icon, color.ToString())
                };
                cmd.ExecuteNonQuery();

                Console.WriteLine("category added");
            }
        }
    }
}
