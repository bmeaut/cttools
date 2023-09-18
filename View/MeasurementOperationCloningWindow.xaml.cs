using Core.Services.Dto;
using Model;
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
using ViewModel;

namespace View
{
    /// <summary>
    /// Interaction logic for MeasurementOperationCloningSelectorWindow.xaml
    /// </summary>
    public partial class MeasurementOperationCloningWindow : Window
    {
        private MeasurementOperationCloningVM _viewModel;

        public MeasurementOperationCloningWindow(MeasurementOperationCloningVM viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedMeasurement != null)
            {
                _viewModel.CloningMeasurementSelected();
                DialogResult = true;
                Close();
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is MaterialSamplesMeasurements)
            {
                var newValue = (MaterialSamplesMeasurements)e.NewValue;
                _viewModel.SelectMaterialSample(newValue.MaterialSample);
            }
            else if (e.NewValue is MeasurementDto)
            {
                _viewModel.SelectMeasurement((MeasurementDto)e.NewValue);
            }
        }
    }
}
