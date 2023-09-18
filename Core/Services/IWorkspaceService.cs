using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Workspaces;

namespace Core.Services
{
    public interface IWorkspaceService
    {
        public Task<Workspace> CreateWorkspaceAsync(Workspace workspace);

        public Task<Workspace> GetWorkspaceByIdAsync(int workspaceId);

        public Task<Workspace> GetWorkspaceByIdWithAllEntitiesAsync(int workspaceId);

        public Task<IEnumerable<Workspace>> ListWorkspacesAsync();

        //public void Archive(int workspaceId);

        public Task DeleteWorkspaceAsync(int workspaceId);

        public Task<Workspace> UpdateWorkspaceAsync(Workspace workspace);

        //public Task ChangeStatusAsync(Status status);
    }
}