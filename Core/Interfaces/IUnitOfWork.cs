using Core.Interfaces.Repositories;
using Core.Operation;
using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        IWorkspaceRepository Workspaces { get; }

        IRepository<Measurement> Measurements { get; }

        IRepository<MaterialScan> MaterialScans { get; }

        IRepository<ScanFile> ScanFiles { get; }

        IRepository<MaterialSample> MaterialSamples { get; }

        IRepository<Status> Statuses { get; }

        IRepository<OperationContext> OperationContexts { get; }

        IRepository<SessionContext> SessionContexts { get; }

        Task<int> CommitAsync();
    }
}
