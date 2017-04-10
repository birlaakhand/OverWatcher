using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using System.IO;

namespace OverWatcher.Commn.Excel
{
    class ExcelParser
    {
        public DataTable ExcelToDataTable(string path)
        {
            var excel = new ExcelPackage();
            excel.Load(File.OpenRead(path));
            var sheet = excel.Workbook.Worksheets.FirstOrDefault();
            return null;
        }
    }
}
