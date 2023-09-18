using Cv4s.UI.ViewModel;
using System.Windows;

namespace Cv4s.UI.View
{
    /// <summary>
    /// Interaction logic for OxyPlotViewer.xaml
    /// </summary>
    public partial class OxyPlotViewer : Window
    {
        private OxyPlotViewerViewModel _viewModel;

        public OxyPlotViewer(OxyPlotViewerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;
        }

        private void ExportToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportToExcel();
        }

        private void ExportToPNGButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportAsImage();
        }
    }
}
