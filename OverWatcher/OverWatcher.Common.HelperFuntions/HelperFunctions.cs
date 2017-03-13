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
    public static class HelperFunctions
    {
        public static string OWSaveToCSV(this DataTable dt, string fileNamePostfx)
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

        public static void OWSort<T>(this DataTable dt, string colName, SortDirection direction)
        {
            DataTable tmp = dt.Clone();
            tmp.Columns[colName].DataType = typeof(T);
            foreach (DataRow dr in dt.Rows)
            {
                tmp.ImportRow(dr);
            }
            tmp.DefaultView.Sort = colName + " " + direction;
            dt.Clear();
            foreach(DataRow dr in tmp.DefaultView.ToTable().Rows)
            {
                dt.ImportRow(dr);
            }
        }

        public static void SortDataTable(this DataTable dt, string colName, SortDirection direction)
        {
            dt.DefaultView.Sort = colName + " " + direction;
            var tmp = dt.DefaultView.ToTable();
            dt.Clear();
            dt.Merge(tmp);
        }
        public static DataTable CSVToDataTable(string strFilePath)
        {
            DataTable dt = new DataTable();
            dt.TableName = Path.GetFileNameWithoutExtension(strFilePath);
            using (StreamReader sr = new StreamReader(strFilePath))
            {
                string[] headers = CSVLineSpliter(sr.ReadLine());
                foreach (string header in headers)
                {
                    dt.Columns.Add(header);
                }
                while (!sr.EndOfStream)
                {
                    string[] rows = CSVLineSpliter(sr.ReadLine());
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i];
                    }
                    dt.Rows.Add(dr);
                }

            }


            return dt;
        }

        private static string[] CSVLineSpliter(string line)
        {
            List<string> buffer = new List<string>();
            bool isQuoted = false;
            StringBuilder stringBuffer = new StringBuilder();
            foreach(char c in line)
            {
                if (c == '\"') isQuoted = !isQuoted;
                else if(!isQuoted && c == ',')
                {
                    buffer.Add(stringBuffer.ToString());
                    stringBuffer.Clear();
                }
                else
                {
                    stringBuffer.Append(c);
                }

            }
            return buffer.ToArray<string>();
        }
    }

    public class Dictionary<TKey1, TKey2, TValue> : Dictionary<Tuple<TKey1, TKey2>, TValue>, IDictionary<Tuple<TKey1, TKey2>, TValue>
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
