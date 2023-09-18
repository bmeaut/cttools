using Core.Interfaces.Repositories;
using Core.Workspaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repositories
{
    public class WorkspaceRepository : Repository<Workspace>, IWorkspaceRepository
    {
        public WorkspaceRepository(DbContext context) : base(context)
        {

        }

        public override IEnumerable<Workspace> Where(Expression<Func<Workspace, bool>> predicate)
        {
            return _context.Set<Workspace>()
                .Include(w => w.CurrentStatus)
                .Include(w => w.Statuses)
                .Include(w => w.SessionContext)
                .Include(w => w.SessionContext.CurrentMeasurement)
                .Where(predicate);
        }

        public override async Task<IEnumerable<Workspace>> GetAllAsync()
        {
            return await _context.Set<Workspace>()
                .Include(w => w.CurrentStatus)
                .Include(w => w.Statuses)
                .Include(w => w.SessionContext)
                .Include(w => w.SessionContext.CurrentMeasurement)
                .ToListAsync();
        }

        public override async Task<Workspace> SingleOrDefaultAsync(Expression<Func<Workspace, bool>> predicate)
        {
            return await _context.Set<Workspace>()
                .Include(w => w.CurrentStatus)
                .Include(w => w.Statuses)
                .Include(w => w.SessionContext)
                .Include(w => w.SessionContext.CurrentMeasurement)
                .SingleOrDefaultAsync(predicate);
        }

        public Task<Workspace> GetByIdWithAllEntitiesAsync(int workspaceId)
        {
            return _context.Set<Workspace>()
                .Include(w => w.CurrentStatus)
                .Include(w => w.Statuses)
                .Include(w => w.MaterialSamples)
                .Include(w => w.MaterialSamples).ThenInclude(ms => ms.MaterialScan).ThenInclude(ms => ms.ScanFiles)
                //.Include(w => w.MaterialSamples).ThenInclude(ms => ms.Measurements).ThenInclude(m => m.OperationContexts)
                .Include(w => w.MaterialSamples).ThenInclude(ms => ms.UserGeneratedFiles)
                .Include(w => w.SessionContext)
                .Include(w => w.SessionContext.CurrentMeasurement)
                .AsSplitQuery()
                .SingleOrDefaultAsync(w => w.Id == workspaceId);
        }
    }
}
