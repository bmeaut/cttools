using Core.Interfaces.Operation;
using Core.Operation;
using Core.Services.Dto;
using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IMeasurementService
    {
        double RunningOperationProgress { get; }

        public Task<Measurement> CreateMeasurementAsync(int materialSampleId, Measurement measurement);

        public Task<Measurement> GetMeasurementByIdAsync(int measurementId);

        public Task<IEnumerable<Measurement>> ListMeasurementsByMaterialSampleAsync(int materialSampleId);

        public Task<Measurement> UpdateMeasurementAsync(Measurement measurement);

        public Task RunOperationAsync(string operationName, OperationProperties operationProperties,
            OperationRunEventArgs operationRunEventArgs);

        public Task<List<OperationContext>> ListOperationContextsByMeasurementIdAsync(int measurementId);

        void CancelCurrentRunningOperation();
        Task<Dictionary<string, InternalOutput>> GetInternalOutputsByMeasurementIdAsync(int measurementId);

        Task<PixelInformationDto> GetPixelInformationAsync(int x, int y);
    }
}
