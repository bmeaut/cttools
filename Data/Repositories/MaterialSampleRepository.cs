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
    public class MaterialSampleRepository : Repository<MaterialSample>
    {
        public MaterialSampleRepository(DbContext context) : base(context)
        {

        }

        public override IEnumerable<MaterialSample> Where(Expression<Func<MaterialSample, bool>> predicate)
        {
            return _context.Set<MaterialSample>()
                .Include(ms => ms.MaterialScan)
                .Include(ms => ms.MaterialScan.ScanFiles)
                .Include(ms => ms.CurrentStatus)
                .Include(ms => ms.Statuses)
                .Include(ms => ms.UserGeneratedFiles)
                .Where(predicate);
        }

        public override async Task<IEnumerable<MaterialSample>> GetAllAsync()
        {
            return await _context.Set<MaterialSample>()
                .Include(ms => ms.MaterialScan)
                .Include(ms => ms.MaterialScan.ScanFiles)
                .Include(ms => ms.CurrentStatus)
                .Include(ms => ms.Statuses)
                .Include(ms => ms.UserGeneratedFiles)
                .ToListAsync();
        }

        public override async Task<MaterialSample> SingleOrDefaultAsync(Expression<Func<MaterialSample, bool>> predicate)
        {
            return await _context.Set<MaterialSample>()
                .Include(ms => ms.MaterialScan)
                .Include(ms => ms.MaterialScan.ScanFiles)
                .Include(ms => ms.CurrentStatus)
                .Include(ms => ms.Statuses)
                .Include(ms => ms.UserGeneratedFiles)
                .SingleOrDefaultAsync(predicate);
        }
    }
}
