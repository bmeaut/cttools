using Core.Services.Dto;
using System;
using System.Collections.Generic;
using System.Text;
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
    /// Interaction logic for MeasurementDetails.xaml
    /// </summary>
    public partial class MeasurementDetails : Window
    {
        private readonly MeasurementDetailsViewModel _viewModel;

        public MeasurementDetails(MeasurementDetailsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        public void SetMaterialSample(MaterialSampleDto materialSample)
        {
            _viewModel.SetMaterialSample(materialSample);
        }

        public void SetMeasurement(MeasurementDto measurement)
        {
            _viewModel.SetMeasurement(measurement);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.SaveMeasurementAsync();
            Close();
        }
    }
}
