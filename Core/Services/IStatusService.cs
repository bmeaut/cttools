using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IStatusService
    {
        public Task<Status> GetStatusByIdAsync(int statusId);

        public Task<Status> UpdateStatusAsync(Status status);

        public Task DeleteStatusAsync(int statusId);

        public Task<Status> CreateStatusForWorkspaceAsync(int workspaceId, Status status);

        public Task<IEnumerable<Status>> GetStatusesForWorkspaceAsync(int workspaceId);

        public Task<Status> GetCurrentStatusForWorkspaceAsync(int workspaceId);

        public Task SetCurrentStatusForWorkspaceAsync(int workspaceId, int statusId);

        public Task<Status> CreateStatusForMaterialSampleAsync(int materialSampleId, Status status);

        public Task<IEnumerable<Status>> GetStatusesForMaterialSampleAsync(int materialSampleId);

        public Task<Status> GetCurrentStatusForMaterialSampleAsync(int materialSampleId);

        public Task SetCurrentStatusForMaterialSampleAsync(int materialSampleId, int statusId);
    }
}
