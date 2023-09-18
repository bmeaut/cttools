using Core.Services.Dto;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for StatusDetails.xaml
    /// </summary>
    public partial class StatusDetails : Window
    {
        private readonly StatusDetailsViewModel _viewModel;

        public StatusDetails(StatusDetailsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        public async Task SetWorkspaceAsync(WorkspaceDto workspace)
        {
            await _viewModel.SetWorkspaceAsync(workspace);
        }

        public async Task SetMaterialSampleAsync(MaterialSampleDto materialSample)
        {
            await _viewModel.SetMaterialSampleAsync(materialSample);
        }

        private void AddNewStatusButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SetCurrentStatus();
            Close();
        }
    }
}
