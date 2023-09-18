using Core.Services;
using Core.Services.Dto;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ViewModel
{
    public class MeasurementDetailsViewModel : ViewModelBase
    {
        private readonly IGatewayService _gatewayService;

        private bool _newMeasurement = true;

        private MaterialSampleDto _materialSample;
        public MaterialSampleDto MaterialSample
        {
            get => _materialSample;
            set
            {
                _materialSample = value;
                OnPropertyChanged();
            }
        }

        private MeasurementDto _measurement = new MeasurementDto();
        public MeasurementDto Measurement
        {
            get => _measurement;
            set
            {
                _measurement = value;
                OnPropertyChanged();
            }
        }

        public MeasurementDetailsViewModel(IGatewayService gatewayService)
        {
            _gatewayService = gatewayService;
        }

        public void SetMaterialSample(MaterialSampleDto materialSample)
        {
            MaterialSample = materialSample;
        }

        public void SetMeasurement(MeasurementDto measurement)
        {
            Measurement = measurement;
            _newMeasurement = false;
        }

        public async Task SaveMeasurementAsync()
        {
            Measurement.MaterialSample = MaterialSample;
            if (_newMeasurement)
            {
                await _gatewayService.CreateMeasurementAsync(Measurement);
            }
            else
            {
                await _gatewayService.UpdateMeasurementAsync(Measurement);
            }
        }
    }
}
