using Core.Exceptions;
using Core.Services;
using Core.Workspaces;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Service.Test
{
    public class SessionServiceTest : ServiceTestBase
    {
        private readonly ISessionService _sessionService;
        private readonly Workspace _workspace;

        public SessionServiceTest()
        {
            _sessionService = new SessionService(_unitOfWork, new WorkspaceService(_unitOfWork));
            _workspace = _unitOfWork.Workspaces.GetAllAsync().Result.First();
        }

        [Fact]
        public void SetCurrentMaterialSample_ThrowsExceptionWhenNoWorkspaceOpened()
        {
            var materialSample = _workspace.MaterialSamples.First();
            Action act = () => _sessionService.SetCurrentMaterialSample(materialSample);
            act.Should().Throw<NoWorkspaceOpenedException>();
        }

        [Fact]
        public void SetCurrentMaterialSample_ThrowsExceptionWhenInvalidMaterialSample()
        {
            _sessionService.SetCurrentWorkspace(_workspace);
            var workspace = _unitOfWork.Workspaces.GetAllAsync().Result.ToList()[1];
            var materialSample = workspace.MaterialSamples.First();

            Action act = () => _sessionService.SetCurrentMaterialSample(materialSample);
            act.Should().Throw<EntityHasNoRelationWithOtherEntityException<Workspace, MaterialSample>>();
        }

        [Fact]
        public void SetCurrentMeasurement_ThrowsExceptionWhenNoWorkspaceOpened()
        {
            var measurement = _unitOfWork.Measurements.GetAllAsync().Result.ToList()[0];
            Action act = () => _sessionService.SetCurrentMeasurement(measurement);
            act.Should().Throw<NoWorkspaceOpenedException>();
        }

        [Fact]
        public void SetCurrentMeasurement_ThrowsExceptionWhenNoMaterialSampleOpened()
        {
            _sessionService.SetCurrentWorkspace(_workspace);
            var measurement = _workspace.MaterialSamples.First().Measurements.First();
            Action act = () => _sessionService.SetCurrentMeasurement(measurement);
            act.Should().Throw<NoMaterialSampleOpenedException>();
        }

        [Fact]
        public void SetCurrentMeasurement_ThrowsExceptionWhenInvalidMeasurement()
        {
            _sessionService.SetCurrentWorkspace(_workspace);
            _sessionService.SetCurrentMaterialSample(_workspace.MaterialSamples.First());

            var workspace = _unitOfWork.Workspaces.GetAllAsync().Result.ToList()[1];
            var materialSample = workspace.MaterialSamples.First();
            var measurement = materialSample.Measurements.First();

            Action act = () => _sessionService.SetCurrentMeasurement(measurement);
            act.Should().Throw<EntityHasNoRelationWithOtherEntityException<MaterialSample, Measurement>>();
        }

        [Fact]
        public void SetCurrentLayer_ThrowsExceptionWhenNoWorkspaceOpened()
        {
            Action act = () => _sessionService.SetCurrentLayer(0);
            act.Should().Throw<NoWorkspaceOpenedException>();
        }

        [Fact]
        public void SetCurrentLayer_ThrowsExceptionWhenNoMaterialSampleOpened()
        {
            _sessionService.SetCurrentWorkspace(_workspace);
            Action act = () => _sessionService.SetCurrentLayer(0);
            act.Should().Throw<NoMaterialSampleOpenedException>();
        }

        [Fact]
        public void SetCurrentLayer_ThrowsExceptionWhenNoMeasurementOpened()
        {
            _sessionService.SetCurrentWorkspace(_workspace);
            _sessionService.SetCurrentMaterialSample(_workspace.MaterialSamples.First());
            Action act = () => _sessionService.SetCurrentLayer(0);
            act.Should().Throw<NoMeasurementOpenedException>();
        }

        [Fact]
        public void SetCurrentLayer_ThrowsExceptionWhenLayerIndexOutOfBounds()
        {
            _sessionService.SetCurrentWorkspace(_workspace);
            _sessionService.SetCurrentMaterialSample(_workspace.MaterialSamples.First());
            _sessionService.SetCurrentMeasurement(_workspace.MaterialSamples.First().Measurements.First());

            Action act = () => _sessionService.SetCurrentLayer(-1);
            act.Should().Throw<LayerIndexOutOfBoundsException>();

            var layer = _workspace.MaterialSamples.First().RawImages.NumberOfLayers + 10;
            act = () => _sessionService.SetCurrentLayer(layer);
            act.Should().Throw<LayerIndexOutOfBoundsException>();
        }

        [Fact]
        public async void SaveCurrentSessionAsync_SavesSessionContext()
        {
            var materialSample = _workspace.MaterialSamples.First();
            var measurement = materialSample.Measurements.First();
            var layer = 1;
            _sessionService.SetCurrentWorkspace(_workspace);
            _sessionService.SetCurrentMaterialSample(materialSample);
            _sessionService.SetCurrentMeasurement(measurement);
            _sessionService.SetCurrentLayer(layer);

            var sessionContext = await _sessionService.SaveCurrentSessionAsync();
            sessionContext.CurrentMeasurement.MaterialSample.Should().Be(materialSample);
            sessionContext.CurrentMeasurement.Should().Be(measurement);
            sessionContext.CurrentLayerIndex.Should().Be(layer);

            var workspace = _unitOfWork.Workspaces.GetAllAsync().Result.First();
            workspace.SessionContext.Should().Be(sessionContext);
        }

        [Fact]
        public async void SaveCurrentSessionAsync_ThrowsExceptionWhenNoCurrentWorkspace()
        {
            Func<Task> act = async () => await _sessionService.SaveCurrentSessionAsync();
            await act.Should().ThrowAsync<NoWorkspaceOpenedException>();
        }
    }
}
