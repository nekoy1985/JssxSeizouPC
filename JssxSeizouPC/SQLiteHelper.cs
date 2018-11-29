using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace JssxSeizouPC
{
    class SqliteHelper
    {
        static string DbPath = Path.Combine(Environment.CurrentDirectory, "DB");

        //与指定的数据库(实际上就是一个文件)建立连接
        private static SQLiteConnection CreateDatabaseConnection(string dbName = null)
        {
            if (!string.IsNullOrEmpty(DbPath) && !Directory.Exists(DbPath))
                Directory.CreateDirectory(DbPath);
            dbName = dbName == null ? "line.db" : dbName;
            var dbFilePath = Path.Combine(DbPath, dbName);
            return new SQLiteConnection("DataSource = " + dbFilePath);
        }

        // 使用全局静态变量保存连接
        private static SQLiteConnection connection = CreateDatabaseConnection();


        // 判断连接是否处于打开状态
        private static void Open(SQLiteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
        }
        public static void ExecuteNonQuery(string sql)
        {
            // 确保连接打开
            Open(connection);

            using (var tr = connection.BeginTransaction())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
                tr.Commit();
            }
        }
        public static void ExecuteQuery(string sql)
        {
            // 确保连接打开
            Open(connection);

            using (var tr = connection.BeginTransaction())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    // 执行查询会返回一个SQLiteDataReader对象
                    var reader = command.ExecuteReader();

                    //reader.Read()方法会从读出一行匹配的数据到reader中。注意：是一行数据。
                    while (reader.Read())
                    {
                        // 有一系列的Get方法，方法的参数是列数。意思是获取第n列的数据，转成Type返回。
                        // 比如这里的语句，意思就是：获取第0列的数据，转成int值返回。
                        var time = reader.GetInt64(0);
                    }
                }
                tr.Commit();
            }
        }
        // 因为SQLite是文件型数据库，可以直接删除文件。但只要数据库连接没有被回收，就无法删除文件。
        public static void DeleteDatabase(string dbName)
        {
            var path = Path.Combine(DbPath, dbName);
            connection.Close();

            // 置空，手动GC，并等待GC完成后执行文件删除。
            connection = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            File.Delete(path);
        }

        public static void Pathchecker()
        {
            if (File.Exists(Environment.CurrentDirectory + "\\DB\\Line.db"))
            {
                //
            }
            else
            {
                File.Create(Environment.CurrentDirectory + "\\DB\\Line.db");
            }
        }
    }
}
