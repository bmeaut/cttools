using Core.Interfaces.Operation;
using Core.Operation;
using Core.Services.Dto;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IGatewayService
    {
        SessionDto GetCurrentSession();

        void SetCurrentLayer(int layer);

        IEnumerable<Bitmap> GetCurrentStateLayers();
        public Task<(Bitmap, string)> GetLayer(int layerId);

        double GetLayerThicknessInMm();

        public Task<PixelInformationDto> GetPixelInformationAsync(int x, int y);

        // ---------------------------------------------- Workspace --------------------------------------------------------------

        Task<WorkspaceDto> CreateWorkspaceAsync(WorkspaceDto workspace);

        Task<WorkspaceDto> UpdateWorkspaceAsync(WorkspaceDto workspace);

        Task<IEnumerable<WorkspaceDto>> ListWorkspacesAsync();
        Task DeleteWorkspaceAsync(int id);

        Task<StatusDto> CreateCurrentStatusForWorkspaceAsync(int workspaceId, StatusDto status);

        Task<IEnumerable<StatusDto>> ListStatusesForWorkspaceAsync(int workspaceId);

        Task OpenWorkspaceAsync(int workspaceId);

        Task OpenWorkspaceLastSessionAsync(int workspaceId);


        // ---------------------------------------------- Material Sample ---------------------------------------------------------
        Task<MaterialSampleDto> CreateMaterialSampleAsync(MaterialSampleDto materialSampleDto);

        Task<MaterialSampleDto> UpdateMaterialSampleAsync(MaterialSampleDto materialSampleDto);

        public Task DeleteMaterialSampleAsync(MaterialSampleDto materialSample);

        Task<IEnumerable<MaterialSampleDto>> ListMaterialSamplesAsync(int workspaceId);

        Task<StatusDto> CreateCurrentStatusForMaterialSampleAsync(int materialSampleId, StatusDto status);

        Task<IEnumerable<StatusDto>> ListStatusesForMaterialSampleAsync(int materialSampleId);

        Task OpenMaterialSampleAsync(int materialSampleId);

        Task<IEnumerable<UserGeneratedFileDto>> ListUserGeneratedFilesForMaterialSampleAsync(int materialSampleId);

        Task<IEnumerable<UserGeneratedFileDto>> UpdateUserGeneratedFilesForMaterialSampleAsync(int materialSampleId, IEnumerable<UserGeneratedFileDto> files);



        Task<MaterialScanDto> CreateMaterialScanAsync(int materialSampleId, MaterialScanDto materialScan);

        Task<MaterialScanDto> UpdateMaterialScanAsync(MaterialScanDto materialScan);



        Task<MeasurementDto> CreateMeasurementAsync(MeasurementDto measurement);

        Task<MeasurementDto> UpdateMeasurementAsync(MeasurementDto measurement);

        Task OpenMeasurementAsync(int measurementId);

        Task<IEnumerable<MeasurementDto>> ListMeasurementsAsync(int materialSampleId);



        Task SaveSessionAsync();


        // ---------------------------------------------- Operations -----------------------------------------------------------
        IEnumerable<OperationDto> ListOperations();

        double RunningOperationProgress { get; }

        Task RunOperationAsync(string operationName, OperationProperties operationProperties,
            OperationRunEventArgs operationRunEventArgs);
        public Task<(bool, bool)> GetOperationDrawSettings(string operationName, OperationProperties operationProperties);

        void CancelCurrentRunningOperation();

        Task UndoOperation();

        Task RedoOperation();

        HistoryDto ListHistory();
        void ClearHistory();

        Task<List<OperationContext>> ListOperationContextsAsync(int measurementId);
        public Task<Dictionary<string, InternalOutput>> ListInternalOutputsAsync(int measurementId);



        Task<byte[]> ExportWorkspaceAsync(int workspaceId);

        Task ImportWorkspaceAsync(byte[] file);


    }
}
