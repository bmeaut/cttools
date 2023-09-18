using Core.Services;
using Core.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModel
{
    public class StatusDetailsViewModel : ViewModelBase
    {
        private readonly IGatewayService _gatewayService;

        private WorkspaceDto _workspace;
        private MaterialSampleDto _materialSample;

        private StatusDto _status = new StatusDto { Id = -1 };
        public StatusDto Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        private string _parentType;
        public string ParentType
        {
            get => _parentType;
            set
            {
                _parentType = value;
                OnPropertyChanged();
            }
        }

        private string _parentName;
        public string ParentName
        {
            get => _parentName;
            set
            {
                _parentName = value;
                OnPropertyChanged();
            }
        }

        private List<StatusDto> _statuses;
        public List<StatusDto> Statuses
        {
            get => _statuses;
            set
            {
                _statuses = value;
                OnPropertyChanged();
            }
        }

        public StatusDetailsViewModel(IGatewayService gatewayService)
        {
            _gatewayService = gatewayService;
        }

        public async Task SetWorkspaceAsync(WorkspaceDto workspace)
        {
            _workspace = workspace;
            ParentType = "Workspace:";
            ParentName = workspace.Name;
            await GetStatusesAsync();
        }

        public async Task SetMaterialSampleAsync(MaterialSampleDto materialSample)
        {
            _materialSample = materialSample;
            ParentType = "Material sample:";
            ParentName = materialSample.Label;
            await GetStatusesAsync();
        }

        public void SetCurrentStatus()
        {
            if (_workspace != null)
            {
                _workspace.CurrentStatus = Status;
            }
            else if (_materialSample != null)
            {
                _materialSample.CurrentStatus = Status;
            }
        }

        private async Task GetStatusesAsync()
        {
            if (_workspace != null)
            {
                Statuses = (await _gatewayService.ListStatusesForWorkspaceAsync(_workspace.Id)).ToList();
            }
            else if (_materialSample != null)
            {
                Statuses = (await _gatewayService.ListStatusesForMaterialSampleAsync(_materialSample.Id)).ToList();
            }
            Statuses = Statuses.OrderBy(s => s.Id).ToList();
            Statuses.Reverse();
            OnPropertyChanged("Statuses");
        }
    }
}
