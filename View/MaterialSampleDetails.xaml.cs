using Core.Services.Dto;
using Microsoft.Win32;
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
using System.Threading;

namespace View
{
    /// <summary>
    /// Interaction logic for MaterialSampleDetails.xaml
    /// </summary>
    public partial class MaterialSampleDetails : Window
    {
        private readonly MaterialSampleDetailsViewModel _viewModel;

        public MaterialSampleDetails(MaterialSampleDetailsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Refresh();
        }

        public void SetWorkspace(WorkspaceDto workspace)
        {
            _viewModel.SetWorkspace(workspace);
        }

        public async Task SetMaterialSampleAsync(MaterialSampleDto materialSample)
        {
            await _viewModel.SetMaterialSampleAsync(materialSample);
        }

        private void RemoveSelectedScanFileButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveSelectedScanFile();
        }

        private void AddScanFilesButton_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;

            var dialog = new OpenFileDialog();
            var filter = "";
            switch (_viewModel.MaterialScan.ScanFileFormat)
            {
                case Core.Enums.ScanFileFormat.DICOM:
                    filter = "DICOM files (.dcm)|*.dcm";
                    break;
                case Core.Enums.ScanFileFormat.PNG:
                    filter = "PNG image files (.png)|*.png";
                    break;
            }
            dialog.Filter = filter;
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == true)
            {
                var filePaths = dialog.FileNames;
                _viewModel.AddScanFiles(filePaths);
            }

            IsEnabled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.MaterialSample.Label == null || _viewModel.MaterialSample.Label == "")
                {
                    _viewModel.LabelRequiredErrorFlag = true;
                    MessageBox.Show("Error occured. Please make sure the label field is set correctly.");
                }
                else
                {
                    _viewModel.LabelRequiredErrorFlag = false;
                    await _viewModel.SaveMaterialSampleAsync();
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured. Please make sure the label field is set correctly.\nError message is:{ex}");
            }

        }

        private void ScanFileTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((e.RemovedItems.Count == 1) && (e.AddedItems.Count == 1))
            {
                _viewModel.RemoveScanFiles();
            }
        }

        private async void EditMaterialSampleStatusButton_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;

            var application = (App)Application.Current;
            var statusDetails = application.ServiceProvider.GetService<StatusDetails>();
            await statusDetails.SetMaterialSampleAsync(_viewModel.MaterialSample);
            statusDetails.Show();
            statusDetails.Closed += StatusDetails_Closed;
        }

        private void StatusDetails_Closed(object sender, EventArgs e)
        {
            _viewModel.RefreshMaterialSample();

            IsEnabled = true;
        }

        private void RemoveSelectedUserGeneratedFileButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveUserGeneratedFile();
        }

        private void AddUserGeneratedFilesButton_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;

            var dialog = new OpenFileDialog();
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == true)
            {
                var filePaths = dialog.FileNames;
                _viewModel.AddUserGeneratedFiles(filePaths);
            }

            IsEnabled = true;
        }
    }
}
