using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.TheICETrade
{
    public enum SortDirection { ASC, DESC };
    class HelperFunctions
    {
        public static string saveDataTableToCSV(string path, DataTable dt, string fileNamePostfx)
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
            string savePath = path + dt.TableName + fileNamePostfx + ".csv";
            File.WriteAllText(savePath, sb.ToString());
            return savePath;
        }
        private static string WrapCSVCell(string cell)
        {
            return cell.Contains(",") ?  "\"" + cell + "\"" : cell;
        }
        public static void SortDataTable(DataTable dt, string colName, SortDirection direction)
        {
            dt.DefaultView.Sort = colName + " " + direction;
            dt = dt.DefaultView.ToTable();
        }

        public static string DataTableToHTML(DataTable dt)
        {
            if (dt.Rows.Count == 0) return ""; // enter code here

            StringBuilder builder = new StringBuilder();
            builder.Append("<html>");
            builder.Append("<head>");
            builder.Append("<title>");
            builder.Append("Page-");
            builder.Append(Guid.NewGuid());
            builder.Append("</title>");
            builder.Append("</head>");
            builder.Append("<body>");
            builder.Append("<table border='1px' cellpadding='5' cellspacing='0' ");
            builder.Append("style='border: solid 1px Silver; font-size: x-small;'>");
            builder.Append("<tr align='left' valign='top'>");
            foreach (DataColumn c in dt.Columns)
            {
                builder.Append("<td align='left' valign='top'><b>");
                builder.Append(c.ColumnName);
                builder.Append("</b></td>");
            }
            builder.Append("</tr>");
            foreach (DataRow r in dt.Rows)
            {
                builder.Append("<tr align='left' valign='top'>");
                foreach (DataColumn c in dt.Columns)
                {
                    builder.Append("<td align='left' valign='top'>");
                    builder.Append(r[c.ColumnName]);
                    builder.Append("</td>");
                }
                builder.Append("</tr>");
            }
            builder.Append("</table>");
            builder.Append("</body>");
            builder.Append("</html>");

            return builder.ToString();
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
