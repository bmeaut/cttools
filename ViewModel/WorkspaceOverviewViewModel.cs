using Core.Services;
using Core.Services.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ViewModel
{
    public class WorkspaceOverviewViewModel : ViewModelBase
    {
        private readonly IGatewayService _gatewayService;

        private IEnumerable<WorkspaceDto> _workspaces;
        public IEnumerable<WorkspaceDto> Workspaces
        {
            get
            {
                return _workspaces;
            }
            set
            {
                _workspaces = value;
                OnPropertyChanged();
            }
        }

        private WorkspaceDto _selectedWorkspace;
        public WorkspaceDto SelectedWorkspace
        {
            get
            {
                return _selectedWorkspace;
            }
            set
            {
                _selectedWorkspace = value;
                OnPropertyChanged();
            }
        }

        public WorkspaceOverviewViewModel(IGatewayService gatewayService)
        {
            _gatewayService = gatewayService;
        }

        public async Task LoadWorkspacesAsync()
        {
            Workspaces = await _gatewayService.ListWorkspacesAsync();
        }

        public async Task OpenLastSessionAsync()
        {
            await _gatewayService.OpenWorkspaceLastSessionAsync(SelectedWorkspace.Id);
        }

        public async Task ExportWorkspace(string fileName)
        {
            var file = await _gatewayService.ExportWorkspaceAsync(SelectedWorkspace.Id);
            WriteByteArrayToFile(fileName, file);
        }

        public async Task ImportWorkspace(string fileName)
        {
            var file = File.ReadAllBytes(fileName);
            await _gatewayService.ImportWorkspaceAsync(file);
        }

        public bool WriteByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process: {0}", ex);
                return false;
            }
        }

        public async Task DeleteWorkspace()
        {
            await _gatewayService.DeleteWorkspaceAsync(SelectedWorkspace.Id);
        }
    }
}
