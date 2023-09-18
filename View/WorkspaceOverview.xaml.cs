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
using Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ViewModel;

namespace View
{
    /// <summary>
    /// Interaction logic for WorkspaceOverviewWindow.xaml
    /// </summary>
    public partial class WorkspaceOverview : Window
    {
        private readonly WorkspaceOverviewViewModel _viewModel;

        public WorkspaceOverview(WorkspaceOverviewViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadWorkspacesAsync();
        }

        private async void NewWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenWorkspaceDetailsAsync(true);
        }

        private async void OpenWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenWorkspaceDetailsAsync(false);
        }

        private async void WorkspacesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedWorkspace != null)
            {
                await OpenWorkspaceDetailsAsync(false);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async Task OpenWorkspaceDetailsAsync(bool newWorkspace)
        {
            var application = (App)Application.Current;
            var workspaceDetails = application.ServiceProvider.GetService<WorkspaceDetails>();
            if (!newWorkspace)
            {
                await workspaceDetails.SetWorkspaceAsync(_viewModel.SelectedWorkspace);
            }
            workspaceDetails.Show();
            Close();
        }

        private async void OpenLastSessionButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.OpenLastSessionAsync();
            var application = (App)Application.Current;
            var measurementEditor = application.ServiceProvider.GetService<MeasurementEditor>();
            measurementEditor.Show();
            Close();
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Korábbi export fájl kiválasztása";
            openFileDialog.Filter = "Compressed Folder (.zip)|*.zip";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    await _viewModel.ImportWorkspace(openFileDialog.FileName);
                }
                catch (WorkspaceImportFileExistsException wsex)
                {
                    MessageBox.Show(wsex.Message);
                }
                await _viewModel.LoadWorkspacesAsync();
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Mentés helyének kiválasztása";
            saveFileDialog.Filter = "Compressed Folder (.zip)|*.zip";
            if (saveFileDialog.ShowDialog() == true)
            {
                string savename = saveFileDialog.FileName.EndsWith(".zip") ? saveFileDialog.FileName : $"{saveFileDialog.FileName}.zip";
                await _viewModel.ExportWorkspace(savename);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Biztosan törli a workspace-t? Minden benne lévő adat elvész!", "Megerősítés", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.DeleteWorkspace();
                await _viewModel.LoadWorkspacesAsync();
            }
        }
    }
}
