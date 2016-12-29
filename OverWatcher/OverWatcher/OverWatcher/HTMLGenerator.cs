using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher
{
    class HTMLGenerator
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static string CountToHTML(string source, int swap, int futures)
        {

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
            builder.Append("style='font-family:arial; border: solid 1px Silver; font-size: x-small;'>");
            builder.Append("<caption align='mid' valign='top' " +
                            "style='border: solid 1px Silver; font-size: medium;'>" +
                            "<b>" + source + " Counts</b></caption>");
            //Column
            builder.Append("<tr align='left' valign='top'>");
            builder.Append("<td align='left'  text-align='center' valign='top'><b>");
            builder.Append("BOOK");
            builder.Append("</b></td>");
            builder.Append("<td align='left'  text-align='center' valign='top'><b>");
            builder.Append("TRADE COUNT");
            builder.Append("</b></td>");
            builder.Append("</tr>");
            //Row
            builder.Append("<tr align='left' valign='top'>");
            builder.Append("<td align='left' valign='top'>");
            builder.Append("Swap");
            builder.Append("</td>");
            builder.Append("<td align='mid' valign='top'>");
            builder.Append(swap);
            builder.Append("</td>");
            builder.Append("</tr>");

            builder.Append("<tr align='left' valign='top'>");
            builder.Append("<td align='left' valign='top'>");
            builder.Append("Futures");
            builder.Append("</td>");
            builder.Append("<td align='mid' valign='top'>");
            builder.Append(futures);
            builder.Append("</td>");
            builder.Append("</tr>");
            //End
            builder.Append("</table>");
            builder.Append("</body>");
            builder.Append("</html>");

            return builder.ToString();
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
            builder.Append("style='font-family:arial; border: solid 1px Silver; font-size: x-small;'>");
            builder.Append("<caption align='mid' valign='top' " +
                "style='border: solid 1px Silver; font-size: medium;'><b>" +
                dt.TableName + "</b></caption>");
            builder.Append("<tr align='left' valign='top'>");
            foreach (DataColumn c in dt.Columns)
            {
                builder.Append("<td align='left' valign='top'><b>");
                builder.Append(c.ColumnName.ToString());
                builder.Append("</b></td>");
            }
            builder.Append("</tr>");
            foreach (DataRow r in dt.Rows)
            {
                builder.Append("<tr align='left' valign='top'>");
                foreach (DataColumn c in dt.Columns)
                {
                    builder.Append("<td align='left' valign='top'>");
                    builder.Append(r[c.ColumnName].ToString());
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
}
