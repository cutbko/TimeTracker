using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;

namespace TimeTracker.Db
{
    public class WrappedSqliteException : Exception
    {
        private readonly string _query;

        public WrappedSqliteException(Exception innerException, SQLiteCommand command)
            : base(innerException.Message, innerException)
        {
            _query = GetQuery(command);
        }

        private static string GetQuery(SQLiteCommand command)
        {
            if (command.Parameters.Count <= 0)
            {
                return command.CommandText;
            }

            var @params = command.Parameters
                .Cast<SqlParameter>()
                .Select(parameter =>
                {
                    var name = "@" + parameter.ParameterName;
                    var value = parameter.SqlDbType == SqlDbType.Int
                        ? parameter.Value != null
                            ? parameter.Value.ToString()
                            : ""
                        : "'" + (parameter.Value != null
                            ? parameter.Value.ToString()
                            : "") + "'";

                    return name + " = " + value;

                });

            return "exec " + command.CommandText + " " + string.Join(", ", @params);
        }

        public override string ToString()
        {
            return "\r\nSql Query: " + _query + "\r\n" + base.ToString();
        }
    }
}