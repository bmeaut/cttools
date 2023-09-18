using Core.Services;
using Core.Services.Dto;
using Model;
using Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ViewModel
{
    public class WorkspaceDetailsViewModel : ViewModelBase
    {
        private readonly IGatewayService _gatewayService;

        private bool _existingWorkspace = false;
        public bool ExistingWorkspace
        {
            get => _existingWorkspace;
            set
            {
                _existingWorkspace = value;
                OnPropertyChanged();
            }
        }

        private WorkspaceDto _workspace = new WorkspaceDto();
        public WorkspaceDto Workspace
        {
            get => _workspace;
            set
            {
                _workspace = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<MaterialSamplesMeasurements> _materialSamplesMeasurements;
        public ObservableCollection<MaterialSamplesMeasurements> MaterialSamplesMeasurements
        {
            get => _materialSamplesMeasurements;
            set
            {
                _materialSamplesMeasurements = value;
                OnPropertyChanged();
            }
        }

        private MaterialSampleDto _selectedMaterialSample;
        public MaterialSampleDto SelectedMaterialSample
        {
            get => _selectedMaterialSample;
            set
            {
                _selectedMaterialSample = value;
                OnPropertyChanged();
            }
        }

        private MeasurementDto _selectedMeasurement;
        public MeasurementDto SelectedMeasurement
        {
            get => _selectedMeasurement;
            set
            {
                _selectedMeasurement = value;
                OnPropertyChanged();
            }
        }

        public WorkspaceDetailsViewModel(IGatewayService gatewayService)
        {
            _gatewayService = gatewayService;
        }

        public async Task SetWorkspaceAsync(WorkspaceDto workspace)
        {
            Workspace = workspace;
            ExistingWorkspace = true;
            await GetMaterialSamplesMeasurementsAsync();
            await _gatewayService.OpenWorkspaceAsync(workspace.Id);
        }

        public async Task OpenMeasurementAsync()
        {
            await _gatewayService.OpenMaterialSampleAsync(SelectedMeasurement.MaterialSample.Id);
            await _gatewayService.OpenMeasurementAsync(SelectedMeasurement.Id);
        }

        public async Task SaveWorkspaceAsync()
        {
            var newWorkspace = Workspace;
            if (!ExistingWorkspace)
            {
                newWorkspace = await _gatewayService.CreateWorkspaceAsync(Workspace);
                ExistingWorkspace = true;
            }
            else
            {
                newWorkspace = await _gatewayService.UpdateWorkspaceAsync(Workspace);
            }

            var newStatus = Workspace.CurrentStatus;
            if (Workspace.CurrentStatus != null && (newWorkspace.CurrentStatus == null || Workspace.CurrentStatus.Id != newWorkspace.CurrentStatus.Id))
            {
                newStatus = await _gatewayService.CreateCurrentStatusForWorkspaceAsync(newWorkspace.Id, Workspace.CurrentStatus);
            }
            Workspace = newWorkspace;
            Workspace.CurrentStatus = newStatus;
            OnPropertyChanged("Workspace");

            await GetMaterialSamplesMeasurementsAsync();
            await _gatewayService.OpenWorkspaceAsync(Workspace.Id);
        }

        public async Task GetMaterialSamplesMeasurementsAsync()
        {
            var materialSamplesMeasurements = new ObservableCollection<MaterialSamplesMeasurements>();

            var materialSamples = await _gatewayService.ListMaterialSamplesAsync(Workspace.Id);
            foreach (var materialSample in materialSamples)
            {
                IEnumerable<MeasurementDto> measurements;
                try
                {
                    measurements = await _gatewayService.ListMeasurementsAsync(materialSample.Id);
                }
                catch (CouldNotLoadDicomFilesException e)
                {
                    MessageBox.Show("Some files did not load successfully.");
                    var remainingFiles = materialSample.MaterialScan.ScanFilePaths.Except(e.Files).ToArray();
                    materialSample.MaterialScan.ScanFilePaths = remainingFiles;
                    if (remainingFiles.Length == 0)
                    {
                        await _gatewayService.DeleteMaterialSampleAsync(materialSample);
                        continue;
                    }
                    await _gatewayService.UpdateMaterialScanAsync(materialSample.MaterialScan);
                    measurements = await _gatewayService.ListMeasurementsAsync(materialSample.Id);
                }
                materialSamplesMeasurements.Add(new MaterialSamplesMeasurements
                {
                    MaterialSample = materialSample,
                    Measurements = measurements.ToList()
                });
            }

            MaterialSamplesMeasurements = materialSamplesMeasurements;
        }

        public void SelectMaterialSample(MaterialSampleDto materialSample)
        {
            SelectedMaterialSample = materialSample;
            SelectedMeasurement = null;
        }

        public void SelectMeasurement(MeasurementDto measurement)
        {
            SelectedMeasurement = measurement;
            SelectedMaterialSample = null;
        }

        public void RefreshWorkspace()
        {
            OnPropertyChanged("Workspace");
        }
    }

}
