using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using OfficeOpenXml;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Cv4s.UI.ViewModel
{
    public class OxyPlotViewerViewModel : ViewModelBase
    {
        private PlotModel _plotModel;

        public PlotModel PlotModel
        {
            get
            {
                return _plotModel;
            }
            set
            {
                _plotModel = value;
                OnPropertyChanged();
            }
        }


        public OxyPlotViewerViewModel(PlotModel model)
        {
            PlotModel = model;
        }


        public void ExportAsImage()
        {
            var fullpath = GetFilePath("png");

            if (fullpath == null)
                return;

            var pngExporter = new OxyPlot.Wpf.PngExporter { Height = 400, Width = 600, Background = OxyColors.White };
            using (var stream = new FileStream(fullpath, FileMode.OpenOrCreate))
            {
                pngExporter.Export(PlotModel,stream);
            }
        }

        
        private string? GetFilePath(string type)
        {
            var dialog = new FolderBrowserDialog();
            string? fullpath = null;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var path = dialog.SelectedPath;
                var filename = $"plot_{DateTimeOffset.Now.ToString("yyyyMMddHHmmss")}.{type}";
                fullpath = Path.Combine(path, filename);
            }

            return fullpath;
        }

        public void ExportToExcel()
        {
            var fullpath = GetFilePath("xlsx");

            if (fullpath == null)
                return;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (ExcelPackage excel = new ExcelPackage())
            {
                var worksheet = excel.Workbook.Worksheets.Add("Worksheet1");

                List<string[]> headerRow = new List<string[]>()
                {
                  new string[] { "X", "Y"}
                };
                string headerRange = "A1:B1";
                worksheet.Cells[headerRange].LoadFromArrays(headerRow);
                worksheet.Cells[headerRange].Style.Font.Bold = true;
                worksheet.Cells[headerRange].Style.Font.Size = 14;

                int rowIndex = 2;
                foreach (var item in _plotModel.Series)
                {
                    List<(double X, double Y)> itemSource = GetItemSource(item);

                    foreach (var point in itemSource)
                    {
                        worksheet.Cells[$"A{rowIndex}"].Value = point.X;
                        worksheet.Cells[$"B{rowIndex}"].Value = point.Y;
                        rowIndex++;
                    }
                    rowIndex += 2;
                }

                FileInfo excelFile = new FileInfo(fullpath);
                excel.SaveAs(excelFile);
            }
        }

        private static List<(double X, double Y)> GetItemSource(Series series)
        {
            List<(double X, double Y)> resultSource = new List<(double X, double Y)>();

            if ((series as ScatterSeries) != null)
            {
                var scatterSeries = series as ScatterSeries;
                foreach (var point in scatterSeries.ItemsSource)
                {
                    var datapoint = point as ScatterPoint;
                    resultSource.Add((datapoint.X, datapoint.Y));
                }

            }
            else if ((series as ColumnSeries) != null)
            {
                var columnSeries = series as ColumnSeries;
                var columnItems = columnSeries.ItemsSource as IEnumerable<ColumnItem>;
                var xAxis = (columnSeries.XAxis as CategoryAxis).ItemsSource as double[];
                for (int i = 0; i < columnItems.Count(); i++)
                {
                    resultSource.Add((xAxis[i], columnItems.ElementAt(i).Value));
                }
            }
            else if ((series as LineSeries) != null)
            {
                var lineSeries = series as LineSeries;
                foreach (var item in lineSeries.ItemsSource)
                {
                    var point = ((DataPoint)item);
                    resultSource.Add((point.X, point.Y));

                }
            }

            return resultSource;
        }
    }
}
