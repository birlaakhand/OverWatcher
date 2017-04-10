using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using OverWatcher.Common.Excel;
using System.Configuration;
using OverWatcher.Common.HelperFunctions;
using OverWatcher.Common.HTML;
using OverWatcher.Common.Logging;

namespace OverWatcher.TradeReconMonitor.Core
{
    static class ExcelProcessor
    {
        #region Members

        private static readonly string[] Headers = {"Cleared Deals", "Futures Deals"};

        private static readonly string[] SelectedCol =
        {
            "Trade Date", "Trade Time", "Deal ID",
            "Leg ID", "Link ID", "B/S", "Product", "Contract", "Price", "Lots", "Trader"
        };

        private static readonly string BasePath;
        private static readonly string OutputPath;
        private static readonly string DownloadPath;

        #endregion

        #region Constructors

        static ExcelProcessor ()
        {
            BasePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            OutputPath = Path.IsPathRooted(ConfigurationManager.AppSettings["OutputFolderPath"])
                ? ConfigurationManager.AppSettings["OutputFolderPath"]
                : BasePath + ConfigurationManager.AppSettings["OutputFolderPath"].Substring(1);
            DownloadPath = Path.IsPathRooted(ConfigurationManager.AppSettings["TempFolderPath"])
                ? ConfigurationManager.AppSettings["TempFolderPath"]
                : BasePath + ConfigurationManager.AppSettings["TempFolderPath"].Substring(1);
            Init();
        }

        #endregion

        private static void Init()
        {
            Logger.Info("Initializing Excel Parser...");           
            CleanUpCsvFolder();
        }

        public static IEnumerable<DataTable> Extract()
        {
            Logger.Info("Analyzing Downloaded Excels and Extracting Data...");
            foreach (var xls in Directory.GetFiles(DownloadPath, "*"
                                                                  +
                                                                  ConfigurationManager.AppSettings["DownloadedFileType"])
            )
            {
                CompanyName company = default(CompanyName);
                foreach (CompanyName name in Enum.GetValues(typeof(CompanyName)))
                {
                    if (Path.GetFileNameWithoutExtension(xls)?.Contains(name.ToString()) ?? false)
                    {
                        company = name;
                    }
                }
                var tableList = new HTMLParser(xls).Process().ToList();
                var cleared = tableList.First(dt => dt.TableName == Headers[0]);
                cleared.TableName = "" + company + ProductType.Swap;               
                var future = tableList.First(dt => dt.TableName == Headers[1]);
                future.TableName = "" + company + ProductType.Futures;
                TrimDataTable(cleared);
                TrimDataTable(future);
                yield return cleared;
                yield return future;

            }
        }

        #region Internal Helpers

        private static void TrimDataTable(System.Data.DataTable dt)
        {
            foreach (string col in dt.Columns.Cast<DataColumn>()
                .Select(x => x.ColumnName)
                .ToArray())
            {
                if (SelectedCol.All(s => s != col))
                {
                    dt.Columns.Remove(col);
                }
            }
        }

        private static void CleanUpCsvFolder()
        {
            System.IO.DirectoryInfo di =
                new DirectoryInfo(Path.IsPathRooted(ConfigurationManager.AppSettings["OutputFolderPath"])
                    ? ConfigurationManager.AppSettings["OutputFolderPath"]
                    : BasePath + ConfigurationManager.AppSettings["OutputFolderPath"].Substring(1));

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }

        #endregion

        public static void DataTableCorrectDate(this System.Data.DataTable dt, string colName)
        {

            foreach (DataRow row in dt.Rows)
            {
                double date = 0L;
                if (double.TryParse(row[colName].ToString(), out date))
                {
                    row[colName] = DateTime
                        .FromOADate(date)
                        .ToString("dd-MM-yyyy");
                }


            }
        }
    }
}
