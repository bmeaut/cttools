using Core.Enums;
using Core.Services;
using Core.Services.Dto;
using Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModel
{
    public class MaterialSampleDetailsViewModel : ViewModelBase
    {
        private readonly IGatewayService _gatewayService;
        private readonly ICommunicationMediator _communicationMediator;
        private bool _newMaterialSample = true;

        private bool _newMaterialScan = true;
        public bool NewMaterialScan => _newMaterialScan;

        private bool _labelRequiredErrorFlag = false;
        public bool LabelRequiredErrorFlag
        {
            get => _labelRequiredErrorFlag;
            set
            {
                _labelRequiredErrorFlag = value;
                OnPropertyChanged();
            }
        }
        #region PNG Reader Bindings
        public ScanFileFormat ScanFileFormat
        {
            get => MaterialScan.ScanFileFormat;
            set
            {
                MaterialScan.ScanFileFormat = value;
                OnPropertyChanged();
                OnPropertyChanged("IsPngTypeSelected");
            }
        }

        public bool IsPngTypeSelected => MaterialScan.ScanFileFormat == ScanFileFormat.PNG;
        public double XResolution
        {
            get
            {
                if (MaterialSample.RawImages == null) return 0;
                return MaterialSample.RawImages.XResolution;
            }

            set
            {
                if (MaterialSample.RawImages == null) return;
                MaterialSample.RawImages.XResolution = value;
                OnPropertyChanged();
            }
        }

        public void Refresh()
        {
            OnPropertyChanged("XResolution");
            OnPropertyChanged("YResolution");
            OnPropertyChanged("ZResolution");
        }

        public double YResolution
        {
            get
            {
                if (MaterialSample.RawImages == null) return 0;
                return MaterialSample.RawImages.YResolution;
            }

            set
            {
                if (MaterialSample.RawImages == null) return;
                MaterialSample.RawImages.YResolution = value;
                OnPropertyChanged();
            }
        }

        public double ZResolution
        {
            get
            {
                if (MaterialSample.RawImages == null) return 0;
                return MaterialSample.RawImages.ZResolution;
            }

            set
            {
                if (MaterialSample.RawImages == null) return;
                MaterialSample.RawImages.ZResolution = value;
                OnPropertyChanged();
            }
        }
        #endregion

        private WorkspaceDto _workspace;
        public WorkspaceDto Workspace
        {
            get => _workspace;
            set
            {
                _workspace = value;
                OnPropertyChanged();
            }
        }

        private MaterialSampleDto _materialSample = new MaterialSampleDto();
        public MaterialSampleDto MaterialSample
        {
            get => _materialSample;
            set
            {
                _materialSample = value;
                OnPropertyChanged();
                Refresh();
            }
        }

        private MaterialScanDto _materialScan = new MaterialScanDto();
        public MaterialScanDto MaterialScan
        {
            get => _materialScan;
            set
            {
                _materialScan = value;
                OnPropertyChanged();
                OnPropertyChanged("IsPngTypeSelected");
            }
        }

        private string _selectedScanFile;
        public string SelectedScanFile
        {
            get => _selectedScanFile;
            set
            {
                if (_newMaterialScan)
                {
                    _selectedScanFile = value;
                    OnPropertyChanged();
                }
            }
        }

        private UserGeneratedFileDto _selectedUserGeneratedFile;
        public UserGeneratedFileDto SelectedUserGeneratedFile
        {
            get => _selectedUserGeneratedFile;
            set
            {
                _selectedUserGeneratedFile = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<UserGeneratedFileDto> _userGeneratedFiles = new ObservableCollection<UserGeneratedFileDto>();
        public ObservableCollection<UserGeneratedFileDto> UserGeneratedFiles
        {
            get => _userGeneratedFiles;
            set
            {
                _userGeneratedFiles = value;
                OnPropertyChanged();
            }
        }

        public MaterialSampleDetailsViewModel(IGatewayService gatewayService, ICommunicationMediator communicationMediator)
        {
            _gatewayService = gatewayService;
            _communicationMediator = communicationMediator;
        }

        public void SetWorkspace(WorkspaceDto workspace)
        {
            Workspace = workspace;
        }

        public async Task SetMaterialSampleAsync(MaterialSampleDto materialSample)
        {
            MaterialSample = materialSample;
            if (materialSample.MaterialScan != null)
            {
                MaterialScan = materialSample.MaterialScan;
                _newMaterialScan = false;
            }
            _newMaterialSample = false;
            Refresh();

            var userGeneratedFiles = await _gatewayService.ListUserGeneratedFilesForMaterialSampleAsync(MaterialSample.Id);
            UserGeneratedFiles = new ObservableCollection<UserGeneratedFileDto>(userGeneratedFiles);
        }

        public void AddScanFiles(string[] filePaths)
        {
            MaterialScan.ScanFilePaths = filePaths;
            OnPropertyChanged("MaterialScan");
        }

        public void RemoveSelectedScanFile()
        {
            if (_materialScan.ScanFilePaths != null)
            {
                _materialScan.ScanFilePaths = _materialScan.ScanFilePaths.Where(sfp => sfp != _selectedScanFile).ToArray();
                OnPropertyChanged("MaterialScan");
                _selectedScanFile = null;
            }
        }

        public void RemoveScanFiles()
        {
            MaterialScan.ScanFilePaths = null;
            OnPropertyChanged("MaterialScan");
        }

        public void AddUserGeneratedFiles(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                var existingFilePaths = UserGeneratedFiles.Select(f => f.Path);
                if (!existingFilePaths.Contains(filePath))
                {
                    UserGeneratedFiles.Add(new UserGeneratedFileDto
                    {
                        Path = filePath
                    });
                }
            }
        }

        public void RemoveUserGeneratedFile()
        {
            if (SelectedUserGeneratedFile != null)
            {
                UserGeneratedFiles.Remove(SelectedUserGeneratedFile);
                SelectedUserGeneratedFile = null;
            }
        }

        public async Task SaveMaterialSampleAsync()
        {
            var newMaterialSample = MaterialSample;
            if (newMaterialSample.Label is null)
                throw new Exception("Cannot create sample without label.");
            if (_newMaterialSample)
            {
                MaterialSample.WorkspaceId = Workspace.Id;
                newMaterialSample = await _gatewayService.CreateMaterialSampleAsync(MaterialSample);
                _communicationMediator.LastCreatedMaterialSampleId = newMaterialSample.Id;
            }
            else
            {
                newMaterialSample = await _gatewayService.UpdateMaterialSampleAsync(MaterialSample);
            }

            if (_newMaterialScan && _materialScan.ScanFilePaths != null)
            {
                await _gatewayService.CreateMaterialScanAsync(newMaterialSample.Id, MaterialScan);
            }

            var newStatus = MaterialSample.CurrentStatus;
            if (MaterialSample.CurrentStatus != null && (newMaterialSample.CurrentStatus == null || MaterialSample.CurrentStatus.Id != newMaterialSample.CurrentStatus.Id))
            {
                newStatus = await _gatewayService.CreateCurrentStatusForMaterialSampleAsync(newMaterialSample.Id, MaterialSample.CurrentStatus);
            }
            MaterialSample = newMaterialSample;
            MaterialSample.CurrentStatus = newStatus;

            if (UserGeneratedFiles.Count != 0)
            {
                await _gatewayService.UpdateUserGeneratedFilesForMaterialSampleAsync(MaterialSample.Id, UserGeneratedFiles);
            }

            OnPropertyChanged("MaterialSample");
        }

        public void RefreshMaterialSample()
        {
            OnPropertyChanged("MaterialSample");
        }
    }
}
