using AutoMapper;
using Core.Exceptions;
using Core.Image;
using Core.Interfaces;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation;
using Core.Operation.InternalOutputs;
using Core.Services;
using Core.Services.Dto;
using Core.Workspaces;
using Dicom.Imaging;
using EvilDICOM.Core.Helpers;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;

namespace Service
{
    public class GatewayService : IGatewayService
    {
        private readonly IMapper _mapper;
        private readonly IWorkspaceService _workspaceService;
        private readonly IMaterialSampleService _materialSampleService;
        private readonly IMaterialScanService _materialScanService;
        private readonly IOperationService _operationService;
        private readonly IMeasurementService _measurementService;
        private readonly ISessionService _sessionService;
        private readonly IHistoryService _historyService;
        private readonly IStatusService _statusService;
        private readonly IExportImportService _exportImportService;

        private readonly IBlobId2ColorConverterService _blobId2ColorConverter;

        public double RunningOperationProgress => _measurementService.RunningOperationProgress;

        public GatewayService(
            IMapper mapper,
            IWorkspaceService workspaceService,
            IMaterialSampleService materialSampleService,
            IMaterialScanService materialScanService,
            IOperationService operationService,
            IMeasurementService measurementService,
            ISessionService sessionService,
            IHistoryService historyService,
            IStatusService statusService,
            IBlobId2ColorConverterService blobId2ColorConverter,
            IExportImportService exportImportService
            )
        {
            _mapper = mapper;
            _workspaceService = workspaceService;
            _materialSampleService = materialSampleService;
            _materialScanService = materialScanService;
            _operationService = operationService;
            _measurementService = measurementService;
            _sessionService = sessionService;
            _historyService = historyService;
            _statusService = statusService;
            _blobId2ColorConverter = blobId2ColorConverter;
            _exportImportService = exportImportService;
        }

        // TODO: validation
        public SessionDto GetCurrentSession()
        {
            var workspace = _mapper.Map<WorkspaceDto>(_sessionService.GetCurrentWorkspace());
            var m = _sessionService.GetCurrentMeasurement();
            var measurement = _mapper.Map<MeasurementDto>(m);

            return new SessionDto
            {
                CurrentWorkspace = workspace,
                CurrentMeasurement = measurement,
                CurrentLayer = _sessionService.GetCurrentLayer()
            };
        }

        public void SetCurrentLayer(int layer)
        {
            int previousLayer = _sessionService.GetCurrentLayer();

            if (layer != previousLayer)
            {
                _sessionService.SetCurrentLayer(layer);

                //_historyService.AddStep(new LayerChangeHistoryStep(_sessionService, previousLayer, layer)); // TODO UX?
            }
        }

        public IEnumerable<Bitmap> GetCurrentStateLayers()
        {
            var measurement = _sessionService.GetCurrentMeasurement();
            var layer = _sessionService.GetCurrentLayer();

            var layers = new List<Bitmap>();
            var blobImage = measurement.BlobImages[layer].GenerateBGRAImage(_blobId2ColorConverter);

            layers.Add(measurement.MaterialSample.RawImages[layer]);
            layers.Add(BitmapConverter.ToBitmap(blobImage));

            return layers;
        }
        public async Task<(Bitmap, string)> GetLayer(int layerId)
        {
            var measurement = _sessionService.GetCurrentMeasurement();
            var layer = _sessionService.GetCurrentLayer();
            IList<ScanFile> list = measurement.MaterialSample.MaterialScan.ScanFiles.ToList();
            var filePath = list[layer].FilePath;

            Bitmap bitmap = null;

            if (layerId == 0)
            {
                bitmap = measurement.MaterialSample.RawImages[layer];
            }
            if(layerId == 1)
            {
                var blobImage = measurement.BlobImages[layer].GenerateBGRAImage(_blobId2ColorConverter);
                bitmap = BitmapConverter.ToBitmap(blobImage);
            }
            if (layerId == 2)
            {
                var InternalOutputs = await ListInternalOutputsAsync(measurement.Id);
                if(InternalOutputs.ContainsKey(DistanceTransformOperation.OutputName))
                {
                    var dtio = (MatOutput)InternalOutputs[DistanceTransformOperation.OutputName]; // as DistanceTransformOutput;
                    Mat mat = dtio.Values[layer];
                    var dtBitmap = dtio.GetBitmap(layer);
                    if (dtBitmap != null)
                        bitmap = dtBitmap; //bitmap = new Bitmap(dtBitmap);
                    //return dtio.Bitmap0;
                }
            }
            if (bitmap != null)
                bitmap = new Bitmap(bitmap);
            return (bitmap, filePath);
        }

        public double GetLayerThicknessInMm()
        {
            var measurement = _sessionService.GetCurrentMeasurement();
            return measurement.MaterialSample.RawImages.ZResolution;
        }

        public async Task<PixelInformationDto> GetPixelInformationAsync(int x, int y)
        {
            var imageSize = _sessionService.GetCurrentMeasurement().BlobImages[0].Size;
            bool xOutOfRange = x < 0 || x >= imageSize.Width;
            bool yOutOfRange = y < 0 || y >= imageSize.Height;
            if (yOutOfRange || xOutOfRange)
            {
                return null;
            }
            return await _measurementService.GetPixelInformationAsync(x, y);
        }


        // TODO: validation
        public async Task<WorkspaceDto> CreateWorkspaceAsync(WorkspaceDto workspace)
        {
            var workspaceEntity = new Workspace
            {
                Name = workspace.Name,
                Description = workspace.Description,
                Customer = workspace.Customer,
                DueDate = workspace.DueDate,
                DayOfArrival = workspace.DayOfArrival,
                Price = workspace.Price
            };
            await _workspaceService.CreateWorkspaceAsync(workspaceEntity);

            return _mapper.Map<WorkspaceDto>(workspaceEntity);
        }

        // TODO: validation
        public async Task<WorkspaceDto> UpdateWorkspaceAsync(WorkspaceDto workspace)
        {
            var workspaceEntity = await _workspaceService.GetWorkspaceByIdAsync(workspace.Id);

            workspaceEntity.Name = workspace.Name;
            workspaceEntity.Description = workspace.Description;
            workspaceEntity.Customer = workspace.Customer;
            workspaceEntity.DueDate = workspace.DueDate;
            workspaceEntity.DayOfArrival = workspace.DayOfArrival;
            workspaceEntity.Price = workspace.Price;

            await _workspaceService.UpdateWorkspaceAsync(workspaceEntity);

            return _mapper.Map<WorkspaceDto>(workspaceEntity);
        }

        public async Task<IEnumerable<WorkspaceDto>> ListWorkspacesAsync()
        {
            var workspaces = await _workspaceService.ListWorkspacesAsync();
            return _mapper.Map<IEnumerable<WorkspaceDto>>(workspaces);
        }

        // TODO: validation
        public async Task<StatusDto> CreateCurrentStatusForWorkspaceAsync(int workspaceId, StatusDto status)
        {
            var statusEntity = new Status
            {
                Name = status.Name,
                Description = status.Description
            };
            statusEntity = await _statusService.CreateStatusForWorkspaceAsync(workspaceId, statusEntity);
            await _statusService.SetCurrentStatusForWorkspaceAsync(workspaceId, statusEntity.Id);

            return _mapper.Map<StatusDto>(statusEntity);
        }

        public async Task<IEnumerable<StatusDto>> ListStatusesForWorkspaceAsync(int workspaceId)
        {
            var statuses = await _statusService.GetStatusesForWorkspaceAsync(workspaceId);
            return _mapper.Map<IEnumerable<StatusDto>>(statuses);
        }

        public async Task OpenWorkspaceAsync(int workspaceId)
        {
            var workspace = await _workspaceService.GetWorkspaceByIdAsync(workspaceId);
            _sessionService.SetCurrentWorkspace(workspace);
        }

        public async Task OpenWorkspaceLastSessionAsync(int workspaceId)
        {
            var workspace = await _workspaceService.GetWorkspaceByIdAsync(workspaceId);
            if (workspace.SessionContext == null)
            {
                throw new WorkspaceHasNoSessionContextException(workspaceId);
            }
            _sessionService.SetCurrentWorkspace(workspace);
            _sessionService
                .SetCurrentMaterialSample(await _materialSampleService.GetMaterialSampleByIdAsync(workspace.SessionContext.CurrentMeasurement.MaterialSampleId));
            _sessionService
                .SetCurrentMeasurement(await _measurementService.GetMeasurementByIdAsync(workspace.SessionContext.CurrentMeasurementId));
            _sessionService.SetCurrentLayer(workspace.SessionContext.CurrentLayerIndex);
        }

        //TODO: validation
        public async Task<MaterialSampleDto> CreateMaterialSampleAsync(MaterialSampleDto materialSampleDto)
        {
            var materialSample = new MaterialSample
            {
                Label = materialSampleDto.Label,
                WorkspaceId = materialSampleDto.WorkspaceId
            };
            materialSample = await _materialSampleService.CreateMaterialSampleAsync(materialSample);

            return _mapper.Map<MaterialSampleDto>(materialSample);
        }

        // TODO: validation
        public async Task<MaterialSampleDto> UpdateMaterialSampleAsync(MaterialSampleDto materialSample)
        {
            var materialSampleEntity = await _materialSampleService.GetMaterialSampleByIdAsync(materialSample.Id);

            materialSampleEntity.Label = materialSample.Label;
            if (materialSample.RawImages != null)
            {
                materialSampleEntity.DicomRange = materialSample.RawImages.DicomRange;
                materialSampleEntity.DicomLevel = materialSample.RawImages.DicomLevel;
            }

            await _materialSampleService.UpdateMaterialSampleAsync(materialSampleEntity);
            _sessionService.SetCurrentMaterialSample(materialSampleEntity);

            return _mapper.Map<MaterialSampleDto>(materialSampleEntity);
        }

        public async Task DeleteMaterialSampleAsync(MaterialSampleDto materialSample)
        {
            await _materialSampleService.DeleteMaterialSampleAsync(materialSample.Id);
        }

        public async Task<IEnumerable<MaterialSampleDto>> ListMaterialSamplesAsync(int workspaceId)
        {
            var materialSamples = await _materialSampleService.ListMaterialSamplesByWorkspaceIdAsync(workspaceId);
            return _mapper.Map<IEnumerable<MaterialSampleDto>>(materialSamples);
        }

        // TODO: validation
        public async Task<StatusDto> CreateCurrentStatusForMaterialSampleAsync(int materialSampleId, StatusDto status)
        {
            var statusEntity = new Status
            {
                Name = status.Name,
                Description = status.Description
            };
            statusEntity = await _statusService.CreateStatusForMaterialSampleAsync(materialSampleId, statusEntity);
            await _statusService.SetCurrentStatusForMaterialSampleAsync(materialSampleId, statusEntity.Id);

            return _mapper.Map<StatusDto>(statusEntity);
        }

        public async Task<IEnumerable<StatusDto>> ListStatusesForMaterialSampleAsync(int materialSampleId)
        {
            var statuses = await _statusService.GetStatusesForMaterialSampleAsync(materialSampleId);
            return _mapper.Map<IEnumerable<StatusDto>>(statuses);
        }

        public async Task OpenMaterialSampleAsync(int materialSampleId)
        {
            var materialSample = await _materialSampleService.GetMaterialSampleByIdAsync(materialSampleId);
            _sessionService.SetCurrentMaterialSample(materialSample);
        }

        public async Task<IEnumerable<UserGeneratedFileDto>> ListUserGeneratedFilesForMaterialSampleAsync(int materialSampleId)
        {
            var userGeneratedFiles = await _materialSampleService.ListUserGeneratedFilesForMaterialSampleAsync(materialSampleId);
            return _mapper.Map<IEnumerable<UserGeneratedFileDto>>(userGeneratedFiles);
        }

        // TODO: validation
        public async Task<IEnumerable<UserGeneratedFileDto>> UpdateUserGeneratedFilesForMaterialSampleAsync(int materialSampleId, IEnumerable<UserGeneratedFileDto> files)
        {
            var userGeneratedFiles = _mapper.Map<IEnumerable<UserGeneratedFile>>(files);
            var result = await _materialSampleService.UpdateUserGeneratedFilesAsync(materialSampleId, userGeneratedFiles);
            return _mapper.Map<IEnumerable<UserGeneratedFileDto>>(result.UserGeneratedFiles);
        }

        // TODO: validation
        public async Task<MaterialScanDto> CreateMaterialScanAsync(int materialSampleId, MaterialScanDto materialScan)
        {
            var materialSample = await _materialSampleService.GetMaterialSampleByIdAsync(materialSampleId);
            var materialScanEntity = new MaterialScan
            {
                MaterialSample = materialSample,
                ScanFileFormat = materialScan.ScanFileFormat
            };

            materialScanEntity.ScanFiles = GetScanFilesFromFilePaths(materialScan.ScanFilePaths);

            materialScanEntity = await _materialScanService.CreateMaterialScanAsync(materialScanEntity);
            return _mapper.Map<MaterialScanDto>(materialScanEntity);
        }

        // TODO: validation
        public async Task<MaterialScanDto> UpdateMaterialScanAsync(MaterialScanDto materialScan)
        {
            var materialScanEntity = await _materialScanService.GetMaterialScanByIdAsync(materialScan.Id);

            materialScanEntity.ScanFileFormat = materialScan.ScanFileFormat;
            materialScanEntity.ScanFiles = GetScanFilesFromFilePaths(materialScan.ScanFilePaths);

            materialScanEntity = await _materialScanService.UpdateMaterialScanAsync(materialScanEntity);
            return _mapper.Map<MaterialScanDto>(materialScanEntity);
        }

        public async Task<MeasurementDto> CreateMeasurementAsync(MeasurementDto measurement)
        {
            var measurementEntity = new Measurement
            {
                Name = measurement.Name,
                Description = measurement.Description
            };
            measurementEntity = await _measurementService.CreateMeasurementAsync(measurement.MaterialSample.Id, measurementEntity);

            return _mapper.Map<MeasurementDto>(measurementEntity);
        }

        public async Task<MeasurementDto> UpdateMeasurementAsync(MeasurementDto measurement)
        {
            var measurementEntity = await _measurementService.GetMeasurementByIdAsync(measurement.Id);

            measurementEntity.Name = measurement.Name;
            measurementEntity.Description = measurement.Description;

            measurementEntity = await _measurementService.UpdateMeasurementAsync(measurementEntity);

            return _mapper.Map<MeasurementDto>(measurementEntity);
        }

        public async Task OpenMeasurementAsync(int measurementId)
        {
            var measurement = await _measurementService.GetMeasurementByIdAsync(measurementId);
            var materialSample = await _materialSampleService.GetMaterialSampleByIdAsync(measurement.MaterialSampleId);
            measurement.MaterialSample = materialSample;
            _sessionService.SetCurrentMeasurement(measurement);
        }

        public async Task<IEnumerable<MeasurementDto>> ListMeasurementsAsync(int materialSampleId)
        {
            var measurements = await _measurementService.ListMeasurementsByMaterialSampleAsync(materialSampleId);
            return _mapper.Map<IEnumerable<MeasurementDto>>(measurements);
        }

        public async Task SaveSessionAsync()
        {
            await _sessionService.SaveCurrentSessionAsync();
        }

        public IEnumerable<OperationDto> ListOperations()
        {
            var operations = _operationService.GetAll();
            return _mapper.Map<IEnumerable<OperationDto>>(operations);
        }

        public async Task RunOperationAsync(string operationName, OperationProperties operationProperties,
            OperationRunEventArgs operationRunEventArgs)
        {
            await _measurementService.RunOperationAsync(operationName, operationProperties,
                operationRunEventArgs);
        }

        public async Task<(bool, bool)> GetOperationDrawSettings(string operationName, OperationProperties operationProperties)
        {
            bool isCallableFromCanvas = _operationService.IsOperationCallableFromCanvas(operationName, operationProperties);
            bool isCallableFromButton = _operationService.IsOperationCallableFromButton(operationName, operationProperties);

            return (isCallableFromCanvas, isCallableFromButton);
        }

        public void CancelCurrentRunningOperation()
        {
            _measurementService.CancelCurrentRunningOperation();
        }

        public async Task UndoOperation()
        {
            _historyService.StepBackward();
        }

        public async Task RedoOperation()
        {
            _historyService.StepForward();
        }

        public HistoryDto ListHistory()
        {
            var list = _historyService.GetHistory();
            return list;
        }

        public async Task<List<OperationContext>> ListOperationContextsAsync(int measurementId)
        {
            return await _measurementService.ListOperationContextsByMeasurementIdAsync(measurementId);
        }

        private ICollection<ScanFile> GetScanFilesFromFilePaths(string[] filePaths)
        {
            var scanFiles = new List<ScanFile>();
            foreach (var filePath in filePaths)
            {
                scanFiles.Add(
                    new ScanFile
                    {
                        FilePath = filePath
                    }
                );
            }
            return scanFiles;
        }

        public async Task<byte[]> ExportWorkspaceAsync(int workspaceId)
        {
            return await _exportImportService.ExportWorkspaceAsync(workspaceId);
        }

        public async Task ImportWorkspaceAsync(byte[] file)
        {
            await _exportImportService.ImportWorkspaceAsync(file);
        }

        public async Task DeleteWorkspaceAsync(int workspaceId)
        {
            await _workspaceService.DeleteWorkspaceAsync(workspaceId);
        }

        public async Task<Dictionary<string, InternalOutput>> ListInternalOutputsAsync(int measurementId)
        {
            var measurement = await _measurementService.GetInternalOutputsByMeasurementIdAsync(measurementId);
            return measurement;
        }

        public void ClearHistory()
        {
            _historyService.ClearHistory();
        }
    }
}
