using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using NPOI;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace OverWatcher.Common.Excel
{
    public class ExcelParser : IDisposable
    {
        #region Private Methods/Fields

        private readonly IWorkbook Workbook;

        private ISheet WorkingSheet { set; get; }
        public Dimension WorkingRange { set; get; }
        public Dimension DefaultRange { private set; get; }

        private Dimension GetAddress(int fromRow, int fromCol, int toRow = -1, int toColumn = -1)
        {
            if (toRow == -1) toRow = WorkingSheet.LastRowNum;
            if (toColumn == -1) toColumn = WorkingSheet.GetRow(WorkingSheet.LastRowNum).LastCellNum;
            return new Dimension(fromRow, fromCol, toRow, toColumn);
        }

        private DataTable ToDataTable(Dimension range, bool hasHeader = true)
        {
            DataTable tbl = new DataTable();
            foreach (var firstRowCell in WorkingSheet.GetRow(range.Start.Row))
            {
                tbl.Columns.Add(hasHeader ? firstRowCell.StringCellValue : $"Column {firstRowCell.ColumnIndex}");
            }
            var startRow = hasHeader ? 2 : 1;
            for (int rowNum = startRow; rowNum <= range.End.Row; rowNum++)
            {
                var wsRow = WorkingSheet.GetRow(rowNum);
                DataRow row = tbl.Rows.Add();
                foreach (var cell in wsRow)
                {
                    CellType type = cell.CellType;
                    if (type == CellType.Numeric)
                    {
                        row[cell.ColumnIndex] = cell.NumericCellValue;
                    }
                    else if (type == CellType.Boolean)
                    {
                        row[cell.ColumnIndex] = cell.BooleanCellValue;
                    }
                    else
                    {
                        row[cell.ColumnIndex] = cell.StringCellValue;
                    }
                }
            }
            return tbl;
        }

#endregion

        public void SetWorkingRange(int fromRow, int fromCol, int toRow = -1, int toColumn = -1, int sheeNum = 0)
        {
            WorkingSheet = Workbook.GetSheetAt(sheeNum);
            WorkingRange = GetAddress(fromRow, fromCol, toRow, toColumn);
        }

        public ExcelParser(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                Workbook = new HSSFWorkbook(stream);
            }
            WorkingRange = GetAddress(0, 0);
            DefaultRange = GetAddress(0, 0);
        }


        public DataTable ToDataTable(bool hasHeader = true)
        {
            return ToDataTable(WorkingRange, hasHeader);
        }

        public DataTable ToDataTable(Dimension addr, int sheetNum = 0, bool hasHeader = true)
        {
            var sheet = Workbook.GetSheetAt(sheetNum);
            return ToDataTable(addr, hasHeader);
        }

        public IEnumerable<Coordinate> SearchValue(object value)
        {
            for (int i = WorkingRange.Start.Row; i < WorkingRange.End.Row; ++i)
            {
                foreach (var cell in WorkingSheet.GetRow(i))
                {
                    if (cell.StringCellValue == value.ToString())
                    {
                        yield return new Coordinate(cell.RowIndex, cell.ColumnIndex);
                    }
                }
            }
        }


        public struct Coordinate
        {
            public int Row { get; set; }
            public int Col { get; set; }

            public Coordinate(int row, int col)
            {
                Row = row;
                Col = col;
            }

            public Coordinate Add(int row, int col)
            {
                Row += row;
                Col += col;
                return this;
            }
        }

        public struct Dimension
        {
            public Coordinate Start { get; set; }

            public Coordinate End { get; set; }

            public Dimension(Coordinate start, Coordinate end)
            {
                Start = start;
                End = end;
            }
            public Dimension(int fromRow, int fromCol, int toRow, int toColumn) 
                : this(new Coordinate(fromRow, fromCol), new Coordinate(toRow, toColumn))
            {
                
            }
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    Workbook.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ExcelParserBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
