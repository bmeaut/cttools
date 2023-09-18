using AutoMapper;
using Core.Enums;
using Core.Exceptions;
using Core.Image;
using Core.Interfaces;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation;
using Core.Services;
using Core.Services.Dto;
using Core.Workspaces;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service
{
    public class MeasurementService : IMeasurementService
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly IMaterialSampleService _materialSampleService;

        private readonly IOperationService _operationService;

        private readonly ISessionService _sessionService;

        private readonly IHistoryService _historyService;

        private double _runningOperationProgress = 1;
        private double _measurementProgress = 1;
        private double _measurementProgressRatio = 0.5;
        public double RunningOperationProgress
        {
            get => (_measurementProgress * _measurementProgressRatio) + (_runningOperationProgress * (1 - _measurementProgressRatio));
            set => _runningOperationProgress = value;
        }

        private CancellationTokenSource cancellationTokenSource;
        public CancellationToken _token { get; private set; }

        public Progress<double> progress = new Progress<double>();

        public MeasurementService(
            IUnitOfWork unitOfWork,
            IMaterialSampleService materialSampleService,
            IOperationService operationService,
            ISessionService sessionService,
            IHistoryService historyService)
        {
            _unitOfWork = unitOfWork;
            _materialSampleService = materialSampleService;
            _operationService = operationService;
            _sessionService = sessionService;
            _historyService = historyService;
            progress.ProgressChanged += OperationProgress_ProgressChanged;
        }

        // TODO: validation
        public async Task<Measurement> CreateMeasurementAsync(int materialSampleId, Measurement measurement)
        {
            var materialSample = await _materialSampleService.GetMaterialSampleByIdAsync(materialSampleId);
            measurement.MaterialSample = materialSample;
            measurement.BlobImages = new BlobImageSource(materialSample.RawImages.ImageWidth, materialSample.RawImages.ImageHeight, materialSample.RawImages.NumberOfLayers);

            await _unitOfWork.Measurements.AddAsync(measurement);
            await _unitOfWork.CommitAsync();

            return measurement;
        }

        public async Task<Measurement> GetMeasurementByIdAsync(int measurementId)
        {
            var measurement = await _unitOfWork.Measurements.SingleOrDefaultAsync(m => m.Id == measurementId);
            if (measurement == null)
            {
                throw new EntityWithIdNotFoundException<Measurement>(measurementId);
            }

            return measurement;
        }

        public async Task<Dictionary<string, InternalOutput>> GetInternalOutputsByMeasurementIdAsync(int measurementId)
        {
            var measurement = await _unitOfWork.Measurements.SingleOrDefaultAsync(m => m.Id == measurementId);
            if (measurement == null)
            {
                throw new EntityWithIdNotFoundException<Measurement>(measurementId);
            }
            var operationsContexts = _unitOfWork.OperationContexts.Where(o => o.MeasurementId == measurementId).ToList();
            Dictionary<string, InternalOutput> result = new();
            Dictionary<string, DateTime> resultCreatedAt = new();

            foreach (var operationsContext in operationsContexts)
            {
                operationsContext.InternalOutputs?.ToList().ForEach(io =>
                {
                    var inDicionary = !result.TryAdd(io.Key, io.Value);
                    if (inDicionary)
                        if (operationsContext.CreatedAt > resultCreatedAt[io.Key])
                            result.TryAdd(io.Key, io.Value);
                    if (!inDicionary)
                        resultCreatedAt.Add(io.Key, operationsContext.CreatedAt);
                });
            }

            return result;
        }

        public async Task<IEnumerable<Measurement>> ListMeasurementsByMaterialSampleAsync(int materialSampleId)
        {
            var materialSample = await _materialSampleService.GetMaterialSampleByIdAsync(materialSampleId);
            return _unitOfWork.Measurements.Where(m => m.MaterialSampleId == materialSample.Id);
        }

        //TODO: async
        public async Task<List<OperationContext>> ListOperationContextsByMeasurementIdAsync(int measurementId)
        {
            return _unitOfWork.OperationContexts.Where(oc => oc.MeasurementId == measurementId).ToList();
        }

        // TODO: validation
        public async Task<Measurement> UpdateMeasurementAsync(Measurement measurement)
        {
            _unitOfWork.Measurements.Update(measurement);
            await _unitOfWork.CommitAsync();
            return measurement;
        }

        public async Task RunOperationAsync(string operationName, OperationProperties operationProperties,
            OperationRunEventArgs operationRunEventArgs)
        {
            cancellationTokenSource = new CancellationTokenSource();
            _token = cancellationTokenSource.Token;
            _measurementProgress = 0;
            _runningOperationProgress = 0;

            var operationContext = CreateOperationContext(operationName, operationProperties, operationRunEventArgs);

            var res = await _operationService.RunOperationAsync(operationContext, _token, progress);

            if (_token.IsCancellationRequested)
            {
                _runningOperationProgress = 1;
                _measurementProgress = 1;
                return;
            }

            List<BlobImageProxy> blobImageProxies = new List<BlobImageProxy>();
            var changedBlobImages = res.BlobImages.Values.Where(bi => bi.IsChanged).ToList();
            int applyProgress = 0;
            
            Parallel.ForEach(changedBlobImages, blobImage =>
            {
                var blobImageProxy = (BlobImageProxy)blobImage;
                BlobImageMemento memento = blobImageProxy.ApplyToOriginal();
                blobImageProxies.Add(memento.CreateUndoProxy());
                _measurementProgress = (applyProgress++) / (double)changedBlobImages.Count;
            });

            /*foreach (var item in changedBlobImages)
            {
                var blobImageProxy = (BlobImageProxy)item;
                BlobImageMemento memento = blobImageProxy.ApplyToOriginal();
                blobImageProxies.Add(memento.CreateUndoProxy());
                _measurementProgress = (applyProgress++) / (double)changedBlobImages.Count;
            }*/

            _historyService.AddStep(new GroupOperationHistoryStep(blobImageProxies, GetDisplayName(operationContext)));

            await _unitOfWork.CommitAsync();
            _runningOperationProgress = 1;
            _measurementProgress = 1;
        }

        private OperationContext CreateOcSnapshot(OperationContext copyFrom)
        {
            return new OperationContext
            {
                OperationName = copyFrom.OperationName,
                OperationProperties = copyFrom.OperationProperties,
                OperationRunEventArgs = copyFrom.OperationRunEventArgs,
                MeasurementId = copyFrom.MeasurementId,
                ActiveLayer = copyFrom.ActiveLayer,
                InternalOutputs = copyFrom.InternalOutputs
            };
        }

        private OperationContext CreateOperationContext(string operationName, OperationProperties operationProperties,
            OperationRunEventArgs operationRunEventArgs)
        {
            var measurement = _sessionService.GetCurrentMeasurement();
            var layer = _sessionService.GetCurrentLayer();

            var rawImagePaths = measurement.MaterialSample.MaterialScan.ScanFiles?.Select(sf => sf.FilePath);

            var operationContext = new OperationContext
            {
                OperationName = operationName,
                OperationProperties = operationProperties,
                OperationRunEventArgs = operationRunEventArgs,
                MeasurementId = measurement.Id,
                ActiveLayer = layer,
                BlobImages = GetBlobImageProxies(measurement.BlobImages.ToDictionary()),
                RawImages = measurement.MaterialSample.RawImages.ToDictionary(),
                RawImageMetadata = CreateRawImageMetadata(measurement.MaterialSample.RawImages, rawImagePaths)
            };

            foreach (var bi in operationContext.BlobImages)
                bi.Value.IsChanged = false;

            return operationContext;
        }

        private void OperationProgress_ProgressChanged(object sender, double progress) => _runningOperationProgress = progress;

        private string GetDisplayName(OperationContext context) => $"{context.OperationName} on layer {context.ActiveLayer}";

        public void CancelCurrentRunningOperation() => cancellationTokenSource.Cancel();

        public async Task<PixelInformationDto> GetPixelInformationAsync(int x, int y)
        {
            var measurement = _sessionService.GetCurrentMeasurement();
            var layer = _sessionService.GetCurrentLayer();
            var blobImage = measurement.BlobImages[layer];
            var blobId = blobImage[y, x];
            var rawImages = measurement.MaterialSample.RawImages;
            var tags = blobId != 0 ? blobImage.GetTagsForBlob(blobId) : new List<ITag<int>>();
            return new PixelInformationDto()
            {
                RawImageValue = rawImages[layer].GetPixel(x, y).R,
                DicomValue = rawImages.GetDicomPixelValue(x, y, layer),
                BlobId = blobId,
                Tags = tags
            };
        }

        private RawImageMetadata CreateRawImageMetadata(IRawImageSource ri, IEnumerable<string> RawImagePaths)
        {
            return new RawImageMetadata()
            {
                ImageHeight = ri.ImageHeight,
                ImageWidth = ri.ImageWidth,
                NumberOfLayers = ri.NumberOfLayers,
                XResolution = ri.XResolution,
                YResolution = ri.YResolution,
                ZResolution = ri.ZResolution,
                RawImagePaths = RawImagePaths
            };
        }

        private IDictionary<int, IBlobImage> GetBlobImageProxies(IDictionary<int, BlobImage> blobImages)
        {
            var result = new Dictionary<int, IBlobImage>();
            foreach (var entry in blobImages)
            {
                result.Add(entry.Key, entry.Value.GetProxy());
            }
            return result;
        }

    }
}
