using Core.Services;
using Core.Services.Dto;
using Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ViewModel
{
    public class MeasurementOperationCloningVM : ViewModelBase
    {
        private readonly IGatewayService _gatewayService;
        private readonly ICommunicationMediator _communicationMediator;
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

        public ObservableCollection<MeasurementDto> _measurements { get; set; }

        public ObservableCollection<MeasurementDto> Measurements
        {
            get => _measurements;
            set
            {
                _measurements = value;
                OnPropertyChanged();
            }
        }


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

        public void CloningMeasurementSelected()
        {
            if(SelectedMeasurement != null)
                _communicationMediator.CloningMeasurementId = SelectedMeasurement.Id;
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

        public MeasurementOperationCloningVM(IGatewayService gatewayService, ICommunicationMediator communicationMediator)
        {
            _gatewayService = gatewayService;
            _communicationMediator = communicationMediator;
            var task = GetMaterialSamplesMeasurementsAsync();
        }

        public async Task SetWorkspaceAsync(WorkspaceDto workspace)
        {
            Workspace = workspace;
            ExistingWorkspace = true;
            await GetMaterialSamplesMeasurementsAsync();
            await _gatewayService.OpenWorkspaceAsync(workspace.Id);
        }


        public async Task GetMaterialSamplesMeasurementsAsync()
        {
            var materialSamplesMeasurements = new ObservableCollection<MaterialSamplesMeasurements>();
            var workspaces = await _gatewayService.ListWorkspacesAsync();
            foreach (var workspace in workspaces)
            {
                var materialSamples = await _gatewayService.ListMaterialSamplesAsync(workspace.Id);
                foreach (var materialSample in materialSamples)
                {
                    var measurements = await _gatewayService.ListMeasurementsAsync(materialSample.Id);
                    materialSample.Label = $"{workspace.Name}.{materialSample.Label}";
                    materialSamplesMeasurements.Add(new MaterialSamplesMeasurements
                    {
                        MaterialSample = materialSample,
                        Measurements = measurements.ToList()
                    });
                }
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


    }
}
