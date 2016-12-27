using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace OverWatcher
{
    sealed class DBConnector
    {
        static readonly string Hostname = ConfigurationManager.AppSettings["DBHostName"];
        static readonly string Port = ConfigurationManager.AppSettings["DBPort"];
        static readonly string Username = ConfigurationManager.AppSettings["DBUserName"];
        static readonly string Pwd = ConfigurationManager.AppSettings["DBPassword"];
        static readonly string SID = ConfigurationManager.AppSettings["DBSID"];
        private static OracleConnection BuildConnection()
        {
            string connectionString = string.Format(
                                       ConfigurationManager.ConnectionStrings["OracleConnectionString"].ConnectionString,
                                       Username,
                                       Pwd,
                                       Hostname,
                                       Port.ToString(),
                                       SID
                );
            OracleConnection connection = new OracleConnection();
            connection.ConnectionString = connectionString;

            try
            {
                connection.Open();
            }
            catch (OracleException ex)
            {
                Console.WriteLine(ex.ToString());
                connection.Dispose();
                connection = null;
                throw ex; 
            }
            return connection;
        }
        public static DataTable MakeQuery(string query, string tableName)
        {
            OracleCommand cmd = new OracleCommand();
            DataTable dataTable = new DataTable();
            dataTable.TableName = tableName;
            try
            {
                var connection = BuildConnection();
                if (connection != null)
                {
                    cmd.Connection = connection;
                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;
                    OracleDataReader reader = cmd.ExecuteReader();
                    var columns = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string col = reader.GetName(i);
                        columns.Add(col);
                        dataTable.Columns.Add(col);
                    }
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var row = dataTable.NewRow();
                            foreach(string col in columns)
                            {
                                row[col] = reader[col];
                                Console.Write(row[col]);
                            }
                            dataTable.Rows.Add(row);
                            Console.WriteLine(0);
                        }
                    }
                    connection.Dispose();
                    connection.Close();
                    return dataTable;
                }

                else
                {
                    throw new MissingMemberException("Database Connection failed");
                }
            }
            catch(Exception ex)
            {
                throw new OracleQueryAbortException(ex.ToString());
            }

        } 
    }
}
