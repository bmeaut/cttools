using Cv4s.UI.ViewModel;
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

namespace Cv4s.UI.View
{
    /// <summary>
    /// Interaction logic for MeasurementEditor.xaml
    /// </summary>
    public partial class MeasurementEditor : Window
    {
        private MeasurementEditorViewModel _viewModel;

        public MeasurementEditor(MeasurementEditorViewModel viewModel)
        {
            _viewModel = viewModel;
            this.DataContext = _viewModel;

            InitializeComponent();

            DrawableCanvas.MeasurementEditorViewModel = viewModel;
            _viewModel.RefreshBackgroundAction =
                DrawableCanvas.SetInkCanvasSize;
            DrawableCanvas.SetInkCanvasSize(_viewModel.Background.Width,
                _viewModel.Background.Height);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Window_Loaded(MeasurementEditorWindow);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
