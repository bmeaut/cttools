using AutoMapper;
using FluentAssertions;
using Core.Services;
using Data;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Core.Workspaces;
using System.Linq;
using Data.Test;
using System.Threading.Tasks;
using Core.Exceptions;
using Moq;
using Core.Operation;
using Core.Interfaces.Operation;
using System.Threading;

using System.Linq.Expressions;
using Core.Interfaces.Image;
using System.Drawing;

namespace Service.Test
{
    public class MeasurementServiceTest : ServiceTestBase
    {
        private readonly MeasurementService _measurementService;
        private readonly SessionService _sessionService;
        private readonly HistoryService _historyService;
        private readonly MaterialSampleService _materialSampleService;
        private readonly Mock<IOperationService> _operationServiceMock;
        // private readonly CancellationToken token = new CancellationToken();
        private readonly IProgress<double> progress = new Progress<double>();

        public MeasurementServiceTest() : base()
        {
            _operationServiceMock = new Mock<IOperationService>();
            _sessionService = new SessionService(_unitOfWork, new WorkspaceService(_unitOfWork));
            _historyService = new HistoryService(null); // TODO: fix
            var workspaceService = new WorkspaceService(_unitOfWork);
            _materialSampleService = new MaterialSampleService(_unitOfWork, workspaceService);

            _measurementService = new MeasurementService(_unitOfWork, _materialSampleService, _operationServiceMock.Object, _sessionService, _historyService);
        }

        [Fact]
        public void CreateMeasurementAsync_CreatesMeasurementWithCorrectName()
        {
            var measurementName = "test 1";
            var materialSamples = _unitOfWork.MaterialSamples.GetAllAsync().Result;
            var materialSample = materialSamples.ToList()[0];
            var oldMeasurementCount = _unitOfWork.Measurements.GetAllAsync().Result.Count();

            var measurement = new Measurement
            {
                Name = measurementName
            };

            var result = _measurementService.CreateMeasurementAsync(materialSample.Id, measurement).Result;
            result.Name.Should().BeEquivalentTo(measurementName);
            result.MaterialSample.Id.Should().Be(materialSample.Id);

            var newMeasurementCount = _unitOfWork.Measurements.GetAllAsync().Result.Count();
            newMeasurementCount.Should().Be(oldMeasurementCount + 1);
        }

        [Fact]
        public async void CreateMeasurementAsync_ThrowsExceptionWhenInvalidMaterialSampleId()
        {
            var measurementName = "test 1";
            var measurement = new Measurement
            {
                Name = measurementName
            };

            Func<Task> act = async ()
                => await _measurementService.CreateMeasurementAsync(-1, measurement);
            await act.Should().ThrowAsync<EntityWithIdNotFoundException<MaterialSample>>();
        }

        [Fact]
        public async void RunOperation_CallsRunOperationOnOperationServiceWithContext()
        {
            var operationName = "TestOperation";
            var operationProperties = new Mock<OperationProperties>().Object;
            var operationRunEventArgs = new Mock<OperationRunEventArgs>().Object;
            var operationContext = new OperationContext {
                OperationName = operationName,
                OperationProperties = operationProperties,
                OperationRunEventArgs = operationRunEventArgs,
                MeasurementId = 0,
                ActiveLayer = 0,
                BlobImages = new Dictionary<int, IBlobImage>()
            };
            _operationServiceMock.Setup(o => o.RunOperationAsync(It.IsAny<OperationContext>(), It.IsAny<CancellationToken>(), _measurementService.progress)).Returns(() => Task.FromResult(operationContext));

            var workspace = (await _unitOfWork.Workspaces.GetAllAsync()).ToList()[0];
            _sessionService.SetCurrentWorkspace(workspace);
            _sessionService.SetCurrentMaterialSample(workspace.MaterialSamples.First());
            _sessionService.SetCurrentMeasurement(workspace.MaterialSamples.First().Measurements.First());
            _sessionService.SetCurrentLayer(0);

            await _measurementService.RunOperationAsync(operationName, operationProperties, operationRunEventArgs);
            _operationServiceMock.Verify(
                o => o.RunOperationAsync(It.Is<OperationContext>(
                    o => o.OperationName == operationName && o.OperationProperties == operationProperties), _measurementService._token, _measurementService.progress), Times.Once());
        }

        [Fact]
        public async void GetMeasurementByIdAsync_ReturnsMeasurementWithCorrectId()
        {
            var measurements = await _unitOfWork.Measurements.GetAllAsync();
            var measurement = measurements.ToList()[^1];

            var result = await _measurementService.GetMeasurementByIdAsync(measurement.Id);
            result.Id.Should().Be(measurement.Id);
        }

        [Fact]
        public async void GetMeasurementByIdAsync_ThrowsExceptionWhenInvalidMeasurementId()
        {
            var measurementId = -1;

            Func<Task> act = async () => await _measurementService.GetMeasurementByIdAsync(measurementId);
            await act.Should().ThrowAsync<EntityWithIdNotFoundException<Measurement>>();
        }

        [Fact]
        public async void ListMeasurementsByMaterialSampleAsync_ListsAllMeasurementsForMaterialSample()
        {
            var materialSamples = await _unitOfWork.MaterialSamples.GetAllAsync();
            var materialSample = materialSamples.ToList()[0];

            var measurements = await _measurementService.ListMeasurementsByMaterialSampleAsync(materialSample.Id);
            measurements.Should().BeEquivalentTo(materialSample.Measurements);
        }

        [Fact]
        public async void ListMeasurementsByMaterialSampleAsync_ThrowsExceptionWhenInvalidMaterialSampleId()
        {
            var materialSampleId = -1;

            Func<Task> act = async () => await _measurementService.ListMeasurementsByMaterialSampleAsync(materialSampleId);
            await act.Should().ThrowAsync<EntityWithIdNotFoundException<MaterialSample>>();
        }

        [Fact]
        public async void UpdateMeasurementAsync_UpdatesMeasurementCorrectly()
        {
            var measurement = (await _unitOfWork.Measurements.GetAllAsync()).ToList()[^1];
            var newMeasurementName = "modified measurement";
            measurement.Name = newMeasurementName;

            var result = await _measurementService.UpdateMeasurementAsync(measurement);
            result.Should().Be(measurement);

            measurement = await _measurementService.GetMeasurementByIdAsync(measurement.Id);
            measurement.Name.Should().Be(newMeasurementName);
        }
    }
}
