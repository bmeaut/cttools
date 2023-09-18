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
    public class StatusServiceTest : ServiceTestBase
    {
        private readonly IStatusService _statusService;
        private readonly List<Workspace> _workspaces;
        private readonly List<MaterialSample> _materialSamples;

        public StatusServiceTest() : base()
        {
            var workspaceService = new WorkspaceService(_unitOfWork);
            var materialSampleService = new MaterialSampleService(_unitOfWork, workspaceService);
            _statusService = new StatusService(_unitOfWork, workspaceService, materialSampleService);

            _workspaces = _unitOfWork.Workspaces.GetAllAsync().Result.ToList();
            _materialSamples = _unitOfWork.MaterialSamples.GetAllAsync().Result.ToList();
        }

        [Fact]
        public async void GetStatusById_ReturnsCorrectStatus()
        {
            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            var status = statuses.ToList()[^1];

            var result = await _statusService.GetStatusByIdAsync(status.Id);
            result.Id.Should().Be(status.Id);
        }

        [Fact]
        public async void GetStatusById_ThrowsExceptionWhenIncorrectStatusId()
        {
            var statusId = -1;
            Func<Task> act = async () => await _statusService.GetStatusByIdAsync(statusId);
            await act.Should().ThrowAsync<EntityWithIdNotFoundException<Status>>();
        }

        [Fact]
        public async void UpdateStatusAsync_UpdatesStatusCorrectly()
        {
            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            var status = statuses.ToList()[^1];
            var statusId = status.Id;

            var updatedStatusName = "Updated status";
            status.Name = updatedStatusName;

            var result = await _statusService.UpdateStatusAsync(status);
            result.Id.Should().Be(statusId);

            status = await _statusService.GetStatusByIdAsync(statusId);
            result.Name.Should().Be(status.Name);
        }

        [Fact]
        public async void CreateStatusForWorkspaceAsync_CreatesStatusForWorkspace()
        {
            var workspaceId = _workspaces[^1].Id;

            var status = new Status()
            {
                Name = "Test status"
            };
            var result = await _statusService.CreateStatusForWorkspaceAsync(workspaceId, status);
            result.Name.Should().Be(status.Name);
        }

        [Fact]
        public async void GetStatusesForWorkspaceAsync_ReturnsAllStatusesForWorkspace()
        {
            var workspace = _workspaces[0];
            var statuses = workspace.Statuses;

            var result = await _statusService.GetStatusesForWorkspaceAsync(workspace.Id);
            result.Should().BeEquivalentTo(statuses);
        }

        [Fact]
        public async void GetStatusesForWorkspaceAsync_ReturnsEmptyStatusList()
        {
            var workspace = _workspaces[^1];
            var statuses = workspace.Statuses;

            var result = await _statusService.GetStatusesForWorkspaceAsync(workspace.Id);
            result.Should().BeNullOrEmpty();
        }

        [Fact]
        public async void SetCurrentStatusForWorkspaceAsync_SetsStatusForWorkspace()
        {
            var workspace = _workspaces[0];
            var status = workspace.Statuses.ToList()[0];

            await _statusService.SetCurrentStatusForWorkspaceAsync(workspace.Id, status.Id);
            workspace.CurrentStatus.Should().Be(status);
        }

        [Fact]
        public async void SetCurrentStatusForWorkspaceAsync_ThrowsExceptionWhenWorkspaceHasNoStatuses()
        {
            var workspace = _workspaces[^1];
            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            var status = statuses.ToList()[0];

            Func<Task> act = async ()
                => await _statusService.SetCurrentStatusForWorkspaceAsync(workspace.Id, status.Id);
            await act.Should().ThrowAsync<EntityHasNoOneToManyRelationship<Workspace, Status>>();
        }

        [Fact]
        public async void SetCurrentStatusForWorkspaceAsync_ThrowsExceptionWhenInvalidStatusId()
        {
            var workspace = _workspaces[0];
            var statusId = -1;

            Func<Task> act = async ()
                => await _statusService.SetCurrentStatusForWorkspaceAsync(workspace.Id, statusId);
            await act.Should().ThrowAsync<EntityWithIdNotFoundException<Status>>();
        }

        [Fact]
        public async void CreateStatusForMaterialSampleAsync_CreatesStatusForMaterialSample()
        {
            var materialSampleId = _materialSamples[^1].Id;

            var status = new Status()
            {
                Name = "Test status"
            };
            var result = await _statusService.CreateStatusForWorkspaceAsync(materialSampleId, status);
            result.Name.Should().Be(status.Name);
        }

        [Fact]
        public async void GetStatusesForMaterialSampleAsync_ReturnsAllStatusesForMaterialSample()
        {
            var materialSample = _materialSamples[0];
            var statuses = materialSample.Statuses;

            var result = await _statusService.GetStatusesForMaterialSampleAsync(materialSample.Id);
            result.Should().BeEquivalentTo(statuses);
        }

        [Fact]
        public async void GetStatusesForMaterialSampleAsync_ReturnsEmptyStatusList()
        {
            var materialSample = _materialSamples[^1];
            var statuses = materialSample.Statuses;

            var result = await _statusService.GetStatusesForMaterialSampleAsync(materialSample.Id);
            result.Should().BeNullOrEmpty();
        }

        [Fact]
        public async void SetCurrentStatusForMaterialSampleAsync_SetsStatusForMaterialSample()
        {
            var workspace = _workspaces[0];
            var status = workspace.Statuses.ToList()[0];

            await _statusService.SetCurrentStatusForWorkspaceAsync(workspace.Id, status.Id);
            workspace.CurrentStatus.Should().Be(status);
        }

        [Fact]
        public async void SetCurrentStatusForMaterialSampleAsync_ThrowsExceptionWhenMaterialSampleHasNoStatuses()
        {
            var materialSample = _materialSamples[^1];
            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            var status = statuses.ToList()[0];

            Func<Task> act = async ()
                => await _statusService.SetCurrentStatusForMaterialSampleAsync(materialSample.Id, status.Id);
            await act.Should().ThrowAsync<EntityHasNoOneToManyRelationship<MaterialSample, Status>>();
        }

        [Fact]
        public async void SetCurrentStatusForMaterialSampleAsync_ThrowsExceptionWhenInvalidStatusId()
        {
            var materialSample = _materialSamples[0];
            var statusId = -1;

            Func<Task> act = async ()
                => await _statusService.SetCurrentStatusForMaterialSampleAsync(materialSample.Id, statusId);
            await act.Should().ThrowAsync<EntityWithIdNotFoundException<Status>>();
        }
    }
}
