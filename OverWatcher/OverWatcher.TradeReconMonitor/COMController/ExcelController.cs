using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Interop.Excel;
using System.Configuration;
using System.IO;
using System.Reflection;
using OverWatcher.Common.Logging;
using System.Runtime.InteropServices;
using System.Data;
using Microsoft.VisualBasic.FileIO;
using OverWatcher.Common.HelperFunctions;
using OverWatcher.Common.Interface;

namespace OverWatcher.TradeReconMonitor.Core
{
    [Obsolete("Not used anymore", true)]
    class ExcelController : COMInterfaceBase
    {
        #region Members
        private static readonly string[] Headers = { "Cleared Deals", "Futures Deals" };
        private static readonly string[] SelectedCol =
            { "Trade Date", "Trade Time", "Deal ID",
                "Leg ID", "Link ID", "B/S", "Product", "Contract", "Price", "Lots", "Trader" };
        private readonly string _basePath;
        private readonly string _outputPath;
        private readonly string _downloadPath;
        private Application _excel;
        private Dictionary<CompanyName, ProductType, Range> _rangeTable;
        #endregion
        #region Constructors
        public ExcelController()
        {
            _basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            _outputPath = Path.IsPathRooted(ConfigurationManager.AppSettings["OutputFolderPath"]) ?
                ConfigurationManager.AppSettings["OutputFolderPath"] :
                _basePath + ConfigurationManager.AppSettings["OutputFolderPath"].Substring(1);
            _downloadPath = Path.IsPathRooted(ConfigurationManager.AppSettings["TempFolderPath"]) ?
                ConfigurationManager.AppSettings["TempFolderPath"] :
                _basePath + ConfigurationManager.AppSettings["TempFolderPath"].Substring(1);
            Init();
            Extract();
        }
        #endregion
        #region COM Creation
        private void Init()
        {
            Logger.Info("Initializing Excel Parser...");
            _rangeTable = new Dictionary<CompanyName, ProductType, Range>();

            CleanUpCsvFolder();
            _excel = new Application();
        }
        private void Extract()
        {
            Logger.Info("Analyzing Downloaded Excels and Extracting Data...");
            foreach (var xls in Directory.GetFiles(_downloadPath, "*"
                + ConfigurationManager.AppSettings["DownloadedFileType"]))
            {
                CompanyName company = default(CompanyName);
                foreach (CompanyName name in Enum.GetValues(typeof(CompanyName)))
                {
                    if (Path.GetFileNameWithoutExtension(xls)?.Contains(name.ToString())??false)
                    {
                        company = name;
                    }
                }
                Workbooks workbooks = GetCOM<Workbooks>(_excel.Workbooks);
                Workbook workbook = GetCOM<Workbook>(workbooks.Open(xls));
                Sheets sheets = GetCOM<Sheets>(workbook.Worksheets);
                Worksheet sheet = GetCOM<Worksheet>(sheets.Item[1] as Worksheet);
                GetRanges(sheet, company);
            }
        }
        private void GetRanges(Worksheet workSheet, CompanyName name)
        {
            Range range = GetCOM<Range>(workSheet.Columns["A", Type.Missing]);
            var firstAddr = StringToAddr(
                GetCOM<Range>(range.Find(Headers[0], Type.Missing, Type.Missing, XlLookAt.xlWhole)).Address);
            var secondAddr = StringToAddr(GetCOM<Range>(range.Find(Headers[1], Type.Missing, Type.Missing, XlLookAt.xlWhole)).Address);
            int last = GetCOM<Range>(GetCOM<Range>(workSheet.Cells).SpecialCells(XlCellType.xlCellTypeLastCell, Type.Missing)).Row;
            _rangeTable[name,ProductType.Swap] = SelectRange(workSheet, firstAddr.Item2 + 2, secondAddr.Item2 - 3);
            _rangeTable[name, ProductType.Futures] = SelectRange(workSheet, secondAddr.Item2 + 2, last);
        }
        private Range SelectRange(Worksheet sheet, int startRow, int endRow)
        {
            int lastCol = GetCOM<Range>(GetCOM<Range>(sheet.Cells).SpecialCells(XlCellType.xlCellTypeLastCell, Type.Missing)).Column;
            var startCell = GetCOM<Range>(sheet.Cells[startRow, 1]);
            var endCell = GetCOM<Range>(sheet.Cells[endRow, lastCol]);
            return GetCOM<Range>(sheet.Range[startCell, endCell]);
        }
        #endregion
        #region Interface
        public List<System.Data.DataTable> GetDataTableList()
        {
            List<System.Data.DataTable> dtList = _rangeTable.GetCollections().Select(s => RangeToDataTable(s.Item1, s.Item2, s.Item3)).ToList();
            foreach (var dt in dtList)
            {
                TrimDataTable(dt);
            }
            return dtList;
        }
        public void SaveAsCSV()
        {
            Logger.Info("Saving Data into CSV..");
            _rangeTable.GetCollections().ForEach(s =>
            {
                RangeToCSV(s.Item1, s.Item2, s.Item3);
                TrimCSV(s.Item1, s.Item2);
            });
        }
        #endregion
        #region Internal Helpers
        private Tuple<string, int> StringToAddr(string addr)
        {
            var address = addr.Split("$".ToCharArray()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            return new Tuple<string, int>(address[0], int.Parse(address[1]));
        }

        private void RangeToCSV(CompanyName name, ProductType type, Range range)
        {
            var target = _excel.Workbooks.Add(Type.Missing);
            Worksheet sheet = target.Sheets[1];
            int lastCol = range.Cells.SpecialCells(XlCellType.xlCellTypeLastCell, Type.Missing).Column;
            int lastRow = sheet.Cells.SpecialCells(XlCellType.xlCellTypeLastCell, Type.Missing).Row;
            var to = sheet.Range[sheet.Cells[1, 1], sheet.Cells[lastRow, lastCol]];
            range.Copy(to);                       
            target.SaveAs(_outputPath 
                + name + type + ".csv", XlFileFormat.xlCSVWindows);
            target.Close(false, Type.Missing, Type.Missing);
        }

        private System.Data.DataTable RangeToDataTable(CompanyName name, ProductType type, Range range)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();
            dataTable.TableName = "" + name + type;
            object[,] rawData = (object[,])range?.Value2;
            if (rawData == null) return dataTable;
            for(int i = 1; i <= rawData.GetLength(1); ++i)
            {
                if (rawData[1, i] == null) continue;
                dataTable.Columns.Add(rawData[1, i].ToString(), rawData[1,i].GetType());
            }
            for(int i = 2; i <= rawData.GetLength(0); ++i)
            {
                DataRow row = dataTable.NewRow();
                for(int j = 1; j <= rawData.GetLength(1); ++j)
                {
                    if (rawData[1, j] == null) continue;
                    row[rawData[1, j].ToString()] = rawData[i, j];
                }
                dataTable.Rows.Add(row);
            }
            return dataTable;
        }

        private void TrimCSV(CompanyName name, ProductType type)
        {
            string path = _outputPath + name + type + ".csv";
            System.Data.DataTable csv = HelperFunctions.CSVToDataTable(path);
            csv.TableName = "" + name + type;
            foreach(DataColumn col in csv.Columns)
            {
                if (SelectedCol.All(s => s != col.ColumnName))
                {
                    csv.Columns.Remove(col);
                }
            }
            csv.OWSaveToCSV("");
        }

        private void TrimDataTable(System.Data.DataTable dt)
        {
            foreach(string col in dt.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray())
            {
                if(SelectedCol.All(s => s != col))
                {
                    dt.Columns.Remove(col);
                }
            }
        }
        private void CleanUpCsvFolder()
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(Path.IsPathRooted(ConfigurationManager.AppSettings["OutputFolderPath"]) ?
                ConfigurationManager.AppSettings["OutputFolderPath"] :
                _basePath + ConfigurationManager.AppSettings["OutputFolderPath"].Substring(1));

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }
        #endregion
        public static void DataTableCorrectDate(ref System.Data.DataTable dt, string colName)
        {
            
            foreach (DataRow row in dt.Rows)
            {
                double date = 0L;
                if(double.TryParse(row[colName].ToString(), out date))
                {
                    row[colName] = DateTime
                                        .FromOADate(date)
                                        .ToString("dd-MM-yyyy");
                }


            }
        }
        #region Clean Up
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                Logger.Info("Closing Excel Parser...");
                // TODO: dispose managed state (managed objects).
                CloseCOM(COMCloseType.Exit);
                if (_excel != null)
                {
                    _excel.Quit();
                    Marshal.FinalReleaseComObject(_excel);
                    _excel = null;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                disposedValue = true;
            }
        }
        protected override void CleanUpSetup()
        {
            ClosableComList.Add(typeof(Workbook));
        }
        #endregion
    }
}
