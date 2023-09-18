using Core.Exceptions;
using Core.Services;
using Core.Workspaces;
using Data;
using Data.Test;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Service.Test
{
    public class WorkspaceServiceTest : ServiceTestBase
    {
        private readonly IWorkspaceService _workspaceService;

        public WorkspaceServiceTest() : base()
        {
            _workspaceService = new WorkspaceService(_unitOfWork);
        }

        [Fact]
        public async void CreateWorkspaceAsync_CreatesWorkspaceWithCorrectProperties()
        {
            var oldWorkspaceCount = (await _unitOfWork.Workspaces.GetAllAsync()).Count();

            var workspace = new Workspace
            {
                Name = "New test workspace",
                Description = "Testing a new workspace"
            };

            var result = await _workspaceService.CreateWorkspaceAsync(workspace);
            result.Should().Be(workspace);

            var newWorkspaceCount = (await _unitOfWork.Workspaces.GetAllAsync()).Count();
            newWorkspaceCount.Should().Be(oldWorkspaceCount + 1);
        }

        [Fact]
        public async void GetWorkspaceByIdAsync_ReturnsCorrectWorkspace()
        {
            var workspaces = await _unitOfWork.Workspaces.GetAllAsync();
            var workspace = workspaces.ToList()[^1];

            var result = await _workspaceService.GetWorkspaceByIdAsync(workspace.Id);
            result.Should().Be(workspace);
        }

        [Fact]
        public async void GetWorkspaceByIdAsync_ThrowsExceptionWhenIncorrectWorkspaceId()
        {
            var workspaceId = -1;

            Func<Task> act = async () => await _workspaceService.GetWorkspaceByIdAsync(workspaceId);
            await act.Should().ThrowAsync<EntityWithIdNotFoundException<Workspace>>();
        }

        [Fact]
        public async void UpdateWorkspaceAsync_UpdatesWorkspaceProperties()
        {
            var workspaces = await _unitOfWork.Workspaces.GetAllAsync();
            var workspace = workspaces.ToList()[^1];
            var workspaceId = workspace.Id;

            var updatedWorkspaceName = "Updated workspace";
            workspace.Name = updatedWorkspaceName;

            var result = await _workspaceService.UpdateWorkspaceAsync(workspace);
            result.Id.Should().Be(workspaceId);

            workspace = await _workspaceService.GetWorkspaceByIdAsync(workspaceId);
            result.Name.Should().Be(workspace.Name);
        }

        [Fact]
        public async void DeleteWorkspaceAsync_Test()
        {
            var oldWorkspaceCount = (await _unitOfWork.Workspaces.GetAllAsync()).Count();

            var workspace = new Workspace
            {
                Name = "New test workspace",
                Description = "Testing a new workspace"
            };

            var result = await _workspaceService.CreateWorkspaceAsync(workspace);

            var newWorkspaceCount = (await _unitOfWork.Workspaces.GetAllAsync()).Count();
            newWorkspaceCount.Should().Be(oldWorkspaceCount + 1);

            await _workspaceService.DeleteWorkspaceAsync(result.Id);

            var afterDeleteCount = (await _unitOfWork.Workspaces.GetAllAsync()).Count();
            afterDeleteCount.Should().Be(oldWorkspaceCount);
        }
    }
}
