using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IMaterialScanService
    {
        public Task<MaterialScan> CreateMaterialScanAsync(MaterialScan materialScan);

        public Task<MaterialScan> GetMaterialScanByIdAsync(int materialScanId);

        public Task<IEnumerable<MaterialScan>> ListAllMaterialScansAsync();

        public Task<MaterialScan> UpdateMaterialScanAsync(MaterialScan materialScan);
    }
}
