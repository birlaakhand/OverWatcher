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
        readonly string Hostname = ConfigurationManager.AppSettings["DBHostName"];
        readonly string Port = ConfigurationManager.AppSettings["DBPort"];
        readonly string Username = ConfigurationManager.AppSettings["DBUserName"];
        readonly string Pwd = ConfigurationManager.AppSettings["DBPassword"];
        readonly string SID = ConfigurationManager.AppSettings["DBSID"];
        private static DBConnector instance;
        private static OracleConnection connection;
        private DBConnector()
        {
            BuildConnection();
        }
        ~DBConnector()
        {
            connection.Dispose();
            connection.Close();
        }
        public static DBConnector Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DBConnector();
                }
                return instance;
            }
        }
        private void BuildConnection()
        {
            string connectionString = string.Format(
                                       ConfigurationManager.ConnectionStrings["OracleConnectionString"].ConnectionString,
                                       Username,
                                       Pwd,
                                       Hostname,
                                       Port.ToString(),
                                       SID
                );
            connection = new OracleConnection();
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
                instance = null;
                throw ex; 
            }
        }
        public DataTable MakeQuery(string query, string tableName)
        {
            OracleCommand cmd = new OracleCommand();
            DataTable dataTable = new DataTable();
            dataTable.TableName = tableName;
            try
            {
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
