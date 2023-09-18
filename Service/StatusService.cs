using Core.Exceptions;
using Core.Interfaces;
using Core.Services;
using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class StatusService : IStatusService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWorkspaceService _workspaceService;
        private readonly IMaterialSampleService _materialSampleService;

        public StatusService(
            IUnitOfWork unitOfWork,
            IWorkspaceService workspaceService,
            IMaterialSampleService materialSampleService)
        {
            _unitOfWork = unitOfWork;
            _workspaceService = workspaceService;
            _materialSampleService = materialSampleService;
        }

        public async Task<Status> GetStatusByIdAsync(int statusId)
        {
            var status = await _unitOfWork.Statuses.SingleOrDefaultAsync(s => s.Id == statusId);
            if (status == null)
            {
                throw new EntityWithIdNotFoundException<Status>(statusId);
            }
            return status;
        }

        // TODO: validation
        public async Task<Status> UpdateStatusAsync(Status status)
        {
            _unitOfWork.Statuses.Update(status);
            await _unitOfWork.CommitAsync();
            return status;
        }

        public async Task DeleteStatusAsync(int statusId)
        {
            var status = await GetStatusByIdAsync(statusId);
            _unitOfWork.Statuses.Remove(status);
            await _unitOfWork.CommitAsync();
        }

        // TODO: validation
        public async Task<Status> CreateStatusForWorkspaceAsync(int workspaceId, Status status)
        {
            var workspace = await _workspaceService.GetWorkspaceByIdAsync(workspaceId);
            await _unitOfWork.Statuses.AddAsync(status);
            status.Workspace = workspace;
            await _unitOfWork.CommitAsync();
            return status;
        }

        public async Task<IEnumerable<Status>> GetStatusesForWorkspaceAsync(int workspaceId)
        {
            var workspace = await _workspaceService.GetWorkspaceByIdAsync(workspaceId);
            return workspace.Statuses;
        }

        public async Task<Status> GetCurrentStatusForWorkspaceAsync(int workspaceId)
        {
            var workspace = await _workspaceService.GetWorkspaceByIdAsync(workspaceId);
            return workspace.CurrentStatus;
        }

        public async Task SetCurrentStatusForWorkspaceAsync(int workspaceId, int statusId)
        {
            var workspace = await _workspaceService.GetWorkspaceByIdAsync(workspaceId);
            if (workspace.Statuses == null || workspace.Statuses.Count == 0)
            {
                throw new EntityHasNoOneToManyRelationship<Workspace, Status>(workspaceId);
            }

            var status = workspace.Statuses.SingleOrDefault(s => s.Id == statusId);
            workspace.CurrentStatus = status ?? throw new EntityWithIdNotFoundException<Status>(statusId);
            _unitOfWork.Workspaces.Update(workspace);
            await _unitOfWork.CommitAsync();
        }

        // TODO: validation
        public async Task<Status> CreateStatusForMaterialSampleAsync(int materialSampleId, Status status)
        {
            var materialSample = await _materialSampleService.GetMaterialSampleByIdAsync(materialSampleId);
            await _unitOfWork.Statuses.AddAsync(status);
            status.MaterialSample = materialSample;
            await _unitOfWork.CommitAsync();
            return status;
        }

        public async Task<IEnumerable<Status>> GetStatusesForMaterialSampleAsync(int materialSampleId)
        {
            var materialSample = await _materialSampleService.GetMaterialSampleByIdAsync(materialSampleId);
            return materialSample.Statuses;
        }

        public async Task<Status> GetCurrentStatusForMaterialSampleAsync(int materialSampleId)
        {
            var materialSample = await _materialSampleService.GetMaterialSampleByIdAsync(materialSampleId);
            return materialSample.CurrentStatus;
        }

        public async Task SetCurrentStatusForMaterialSampleAsync(int materialSampleId, int statusId)
        {
            var materialSample = await _materialSampleService.GetMaterialSampleByIdAsync(materialSampleId);
            if (materialSample.Statuses == null || materialSample.Statuses.Count == 0)
            {
                throw new EntityHasNoOneToManyRelationship<MaterialSample, Status>(materialSampleId);
            }

            var status = materialSample.Statuses.SingleOrDefault(s => s.Id == statusId);
            materialSample.CurrentStatus = status ?? throw new EntityWithIdNotFoundException<Status>(statusId);
            _unitOfWork.MaterialSamples.Update(materialSample);
            await _unitOfWork.CommitAsync();
        }
    }
}
