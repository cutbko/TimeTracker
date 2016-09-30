using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TimeTracker.Db
{
    public class DataCommand
    {
        private string _filePath = Path.Combine(Const.AppDataPath, "db.sqlite");
        private string _connectionString;

        public DataCommand()
        {
            _connectionString = "Data Source=" + _filePath;
        }

        public string Hash(string password)
        {
            using (var sha1 = SHA1.Create())
            {
                return Convert.ToBase64String(sha1.ComputeHash(Encoding.Default.GetBytes(password)));
            }
        }

        public bool DbExists()
        {
            return File.Exists(_filePath);
        }

        public void CreateDbFile()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            File.Create(_filePath).Dispose();
        }

        public DataTable GetTable(string query, params SQLiteParameter[] parameters)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                var command = connection.CreateCommand();

                try
                {
                    command.Parameters.AddRange(parameters);
                    command.CommandText = query;

                    connection.Open();

                    using (var adapter = new SQLiteDataAdapter(command))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
                catch (SQLiteException exception)
                {
                    throw new WrappedSqliteException(exception, command);
                }
                finally
                {
                    command.Dispose();
                }
            }
        }

        public DataSet GetSet(string query, params SQLiteParameter[] parameters)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                var command = connection.CreateCommand();

                try
                {
                    command.Parameters.AddRange(parameters);
                    command.CommandText = query;

                    connection.Open();

                    using (var adapter = new SQLiteDataAdapter(command))
                    {
                        var dataSet = new DataSet();
                        adapter.Fill(dataSet);
                        return dataSet;
                    }
                }
                catch (SQLiteException exception)
                {
                    throw new WrappedSqliteException(exception, command);
                }
                finally
                {
                    command.Dispose();
                }
            }
        }

        public T ExecuteScalar<T>(string query, params SQLiteParameter[] parameters)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                var command = connection.CreateCommand();

                try
                {
                    command.Parameters.AddRange(parameters);
                    command.CommandText = query;

                    connection.Open();

                    var execResult = command.ExecuteScalar();

                    var result = execResult != null && execResult != DBNull.Value
                        ? (T)execResult
                        : default(T);

                    return result;
                }
                catch (SQLiteException exception)
                {
                    throw new WrappedSqliteException(exception, command);
                }
                finally
                {
                    command.Dispose();
                }
            }
        }

        public int ExecuteAsNonQuery(string query, params SQLiteParameter[] parameters)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                var command = connection.CreateCommand();

                try
                {
                    command.Parameters.AddRange(parameters);
                    command.CommandText = query;

                    connection.Open();

                    return command.ExecuteNonQuery();
                }
                catch (SQLiteException exception)
                {
                    throw new WrappedSqliteException(exception, command);
                }
                finally
                {
                    command.Dispose();
                }
            }
        }

        public int ExecuteAsNonQueryCustomTimeout(string query, int timeout, params SqlParameter[] parameters)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                var command = connection.CreateCommand();

                try
                {
                    command.Parameters.AddRange(parameters);
                    command.CommandText = query;
                    command.CommandTimeout = timeout;

                    connection.Open();

                    return command.ExecuteNonQuery();
                }
                catch (SQLiteException exception)
                {
                    throw new WrappedSqliteException(exception, command);
                }
                finally
                {
                    command.Dispose();
                }
            }
        }
    }
}