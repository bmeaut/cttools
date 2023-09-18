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
using Microsoft.Extensions.DependencyInjection;
using ViewModel;
using System.Threading.Tasks;
using Model;

namespace View
{
    /// <summary>
    /// Interaction logic for WorkspaceDetails.xaml
    /// </summary>
    public partial class WorkspaceDetails : Window
    {
        private readonly WorkspaceDetailsViewModel _viewModel;
        private int last_opened_id;

        public WorkspaceDetails(WorkspaceDetailsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        public async Task SetWorkspaceAsync(WorkspaceDto workspace)
        {
            await _viewModel.SetWorkspaceAsync(workspace);
        }

        private async void SaveWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.SaveWorkspaceAsync();
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

        private async void NewMaterialSampleButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenMaterialSampleDetailsAsync(true);
        }

        private async void EditMaterialSampleButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenMaterialSampleDetailsAsync(false);
        }

        private void NewMeasurementButton_Click(object sender, RoutedEventArgs e)
        {
            OpenMeasurementDetails(true);
        }

        private void EditMeasurementButton_Click(object sender, RoutedEventArgs e)
        {
            OpenMeasurementDetails(false);
        }

        private async void OpenMeasurement_Click(object sender, RoutedEventArgs e)
        {
            await OpenMeasurementAsync();
        }

        private async Task OpenMeasurementAsync()
        {
            await _viewModel.OpenMeasurementAsync();
            var application = (App)Application.Current;
            var measurementEditor = application.ServiceProvider.GetService<MeasurementEditor>();
            measurementEditor.ShowDialog();
            //Close();
        }

        private async Task OpenMaterialSampleDetailsAsync(bool newMaterialSample)
        {
            IsEnabled = false;
            var application = (App)Application.Current;
            var materialSampleDetails = application.ServiceProvider.GetService<MaterialSampleDetails>();
            materialSampleDetails.SetWorkspace(_viewModel.Workspace);
            if (!newMaterialSample)
            {
                if (_viewModel.SelectedMaterialSample != null)
                    last_opened_id = _viewModel.SelectedMaterialSample.Id;
                await materialSampleDetails.SetMaterialSampleAsync(_viewModel.SelectedMaterialSample);
            }
            materialSampleDetails.Show();
            materialSampleDetails.Closed += MaterialSampleDetails_Closed;
        }

        private async void MaterialSampleDetails_Closed(object sender, EventArgs e)
        {

            await _viewModel.GetMaterialSamplesMeasurementsAsync();
            foreach (var item in _viewModel.MaterialSamplesMeasurements)
            {
                if (item.MaterialSample.Id == last_opened_id)
                    item.IsExpanded = true;
            }
            IsEnabled = true;
        }

        private void OpenMeasurementDetails(bool newMeasurement)
        {
            IsEnabled = false;
            var application = (App)Application.Current;
            var measurementDetails = application.ServiceProvider.GetService<MeasurementDetails>();
            measurementDetails.SetMaterialSample(_viewModel.SelectedMaterialSample);
            if (!newMeasurement)
            {
                measurementDetails.SetMaterialSample(_viewModel.SelectedMeasurement.MaterialSample);
                measurementDetails.SetMeasurement(_viewModel.SelectedMeasurement);
            }
            measurementDetails.Show();
            measurementDetails.Closed += MeasurementDetails_Closed;
        }

        private async void MeasurementDetails_Closed(object sender, EventArgs e)
        {
            await _viewModel.GetMaterialSamplesMeasurementsAsync();
            IsEnabled = true;
        }

        private async void EditWorkspaceStatusButton_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;

            var application = (App)Application.Current;
            var statusDetails = application.ServiceProvider.GetService<StatusDetails>();
            await statusDetails.SetWorkspaceAsync(_viewModel.Workspace);
            statusDetails.Show();
            statusDetails.Closed += StatusDetails_Closed;
        }

        private void StatusDetails_Closed(object sender, EventArgs e)
        {
            _viewModel.RefreshWorkspace();

            IsEnabled = true;
        }

        private async void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedMeasurement != null)
            {
                await OpenMeasurementAsync();
            }
        }

        private void BackToWorkspaceOverViewButton_Click(object sender, RoutedEventArgs e)
        {
            var application = (App)Application.Current;
            var workspaceOverview = application.ServiceProvider.GetService<WorkspaceOverview>();
            workspaceOverview.Show();
            Close();
        }
    }
}
