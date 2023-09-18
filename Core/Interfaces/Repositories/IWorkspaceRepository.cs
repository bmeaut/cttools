using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.Repositories
{
    public interface IWorkspaceRepository : IRepository<Workspace>
    {
        Task<Workspace> GetByIdWithAllEntitiesAsync(int workspaceId);
    }
}
