using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OverWatcher.Common.HTML
{
    public class HTMLParser
    {
        private string _path;
        private readonly string _content;
        private const string TableExpression = "<TABLE[^>]*>(.*?)</TABLE>";
        private const string HeaderExpression = "<TH[^>]*>(.*?)</TH>";
        private const string RowExpression = "<TR[^>]*>(.*?)</TR>";
        private const string ColumnExpression = "<TD[^>]*>(.*?)</TD>";
        public HTMLParser(string path)
        {
            _path = path;
            _content = File.ReadAllText(_path);
        }

        public IEnumerable<DataTable> Process()
        {
            string body = _content
                .Split(new string[] {"<body>"}, StringSplitOptions.RemoveEmptyEntries)[1]
                .Split(new string[] { "</body>" }, StringSplitOptions.RemoveEmptyEntries)[0]
                .Replace("<br/>", string.Empty)
                .Replace("\n", string.Empty);
            string[] tables = body.Split(new string[] {"</table>"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var table in tables)
            {
                yield return StringToDataTable(table);
            }
        }

        private DataTable StringToDataTable(string dt)
        {
            dt = dt + " </table>";
            DataTable table = new DataTable {TableName = dt.Substring(0, dt.IndexOf("<", StringComparison.Ordinal))};
            foreach (Match header in Regex.Matches(dt, HeaderExpression, RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                table.Columns.Add(header.Groups[1].Value);
            }
            dt = dt.Split(new string[] {"</th></tr>"}, StringSplitOptions.RemoveEmptyEntries)[1];
            foreach (Match row in Regex.Matches(dt, RowExpression, RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                var dr = table.NewRow();
                int i = 0;
                foreach (Match cell in Regex.Matches(row.Groups[1].Value, ColumnExpression,
                    RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase))
                {
                    dr[i++] = cell.Groups[1].Value;
                }
                table.Rows.Add(dr);
            }
            return table;
        }
    }
}
