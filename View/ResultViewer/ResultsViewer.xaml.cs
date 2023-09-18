
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Core.Interfaces.Operation;
using Core.Operation.InternalOutputs;
using Core.Operation;
using ViewModel;
using Microsoft.Win32;
using View.ResultViewer;

namespace View
{
    /// <summary>
    /// Interaction logic for ResultsViewer.xaml
    /// </summary>
    public partial class ResultsViewer : Window
    {
        private ResultViewerViewModel _model;
        private InternalOutputType currentInternalOutputType = InternalOutputType.NOT_SUPPORTED;


        public ResultsViewer(ResultViewerViewModel model)
        {
            InitializeComponent();
            _model = model;
            DataContext = model;
            _model.RefreshContentAction += RefreshContent;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _model.OnWindowLoaded();
        }

        private void RefreshContent(int index, InternalOutputType content_type)
        {
            currentInternalOutputType = content_type;
            switch (content_type)
            {
                case InternalOutputType.PORE_DISTRIBUTION:
                case InternalOutputType.OXYPLOT:
                    _model.FrameContent = GetOxyPlotPageForIndex(index, content_type);
                    break;
                case InternalOutputType.NOT_SUPPORTED:
                default:
                    _model.FrameContent = new ResultTypeNotSupportedPage();
                    break;
            }
        }

        private Page GetOxyPlotPageForIndex(int index, InternalOutputType content_type)
        {
            OxyPlotPage page;
            if (_model.Pages.ContainsKey(index))
                return _model.Pages[index];

            if(content_type == InternalOutputType.PORE_DISTRIBUTION)
                page = new OxyPlotPage(((PoreDistributionInternalOutput)_model.InternalOutputs[index]).PlotModel);
            else
                page = new OxyPlotPage(((OxyplotInternalOutput)_model.InternalOutputs[index]).PlotModel);

            _model.Pages.Add(index, page);
            return page;
        }

        private void PreviousPlotButton_Click(object sender, RoutedEventArgs e)
        {
            _model.CurrentPlotIndex--;
        }
        private void NextPlotButton_Click(object sender, RoutedEventArgs e)
        {
            _model.CurrentPlotIndex++;
        }

        private void ExportToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            switch (currentInternalOutputType)
            {
                case InternalOutputType.OXYPLOT:
                    oxyPlotExport(ExportType.EXCEL);
                    break;
                case InternalOutputType.NOT_SUPPORTED:
                default:
                    MessageBox.Show("Content type not supported.");
                    break;
            }
        }


        private void ExportToPNGButton_Click(object sender, RoutedEventArgs e)
        {
            switch (currentInternalOutputType)
            {
                case InternalOutputType.OXYPLOT:
                    oxyPlotExport(ExportType.PNG);
                    break;
                case InternalOutputType.NOT_SUPPORTED:
                default:
                    MessageBox.Show("Content type not supported.");
                    break;
            }

        }

        private void oxyPlotExport(ExportType type)
        {
            if (_model.CurrentPlotIndex < 1)
            {
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Mentés helyének kiválasztása";
            saveFileDialog.Filter = type == ExportType.EXCEL ? "Excel |*.xlsx" : "Png |*.png";

            if (saveFileDialog.ShowDialog() == true)
            {
                string savename="";
                switch (type)
                {
                    case ExportType.EXCEL:
                        savename = saveFileDialog.FileName.EndsWith(".xlsx") ? saveFileDialog.FileName : $"{saveFileDialog.FileName}.xlsx";
                        OxyPlotDataExporter.ExportToExcel(savename, ((OxyplotInternalOutput)_model.InternalOutputs[_model.CurrentPlotIndex - 1]).PlotModel);
                        break;
                    case ExportType.PNG:
                        savename = saveFileDialog.FileName.EndsWith(".png") ? saveFileDialog.FileName : $"{saveFileDialog.FileName}.png";
                        OxyPlotDataExporter.ExportToPNG(((OxyplotInternalOutput)_model.InternalOutputs[_model.CurrentPlotIndex - 1]),savename);
                        break;
                    default:
                        MessageBox.Show("An Error Occured during export! (Wrong Export Type)");
                        break;
                }
            }
        }

       public enum ExportType
        {
            PNG,
            EXCEL
        }

    }
}
