using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace OverWatcher.Common.HelperFunctions
{
    public enum SortDirection { ASC, DESC };
    class HelperFunctions
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
                (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static string SaveDataTableToCSV(DataTable dt, string fileNamePostfx)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dt.Columns.Cast<System.Data.DataColumn>().
                                              Select(column => WrapCSVCell(column.ColumnName));
            sb.AppendLine(string.Join(",", columnNames));

            foreach (System.Data.DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => WrapCSVCell(field.ToString()));
                sb.AppendLine(string.Join(",", fields));
            }
            string savePath = ConfigurationManager.AppSettings["OutputFolderPath"] + dt.TableName + fileNamePostfx + ".csv";
            File.WriteAllText(savePath, sb.ToString());
            return savePath;
        }
        private static string WrapCSVCell(string cell)
        {
            return cell.Contains(",") ?  "\"" + cell + "\"" : cell;
        }
        public static void SortDataTable(ref DataTable dt, string colName, SortDirection direction)
        {
            dt.DefaultView.Sort = colName + " " + direction;
            dt = dt.DefaultView.ToTable();
        }
    }
    class Dictionary<TKey1, TKey2, TValue> : Dictionary<Tuple<TKey1, TKey2>, TValue>, IDictionary<Tuple<TKey1, TKey2>, TValue>
    {

        public TValue this[TKey1 key1, TKey2 key2]
        {
            get { return base[Tuple.Create(key1, key2)]; }
            set { base[Tuple.Create(key1, key2)] = value; }
        }

        public void Add(TKey1 key1, TKey2 key2, TValue value)
        {
            base.Add(Tuple.Create(key1, key2), value);
        }

        public bool ContainsKey(TKey1 key1, TKey2 key2)
        {
            return base.ContainsKey(Tuple.Create(key1, key2));
        }

        public List<Tuple<TKey1, TKey2, TValue>> GetCollections()
        {
            return this.Select(pair => new Tuple<TKey1, TKey2, TValue>(pair.Key.Item1, pair.Key.Item2, pair.Value)).ToList();
        }
    }
}
