using Core.Exceptions;
using Core.Interfaces;
using Core.Services;
using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class MaterialScanService : IMaterialScanService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MaterialScanService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // TODO: validation
        public async Task<MaterialScan> CreateMaterialScanAsync(MaterialScan materialScan)
        {
            await _unitOfWork.MaterialScans.AddAsync(materialScan);
            await _unitOfWork.CommitAsync();
            return materialScan;
        }

        public async Task<MaterialScan> GetMaterialScanByIdAsync(int materialScanId)
        {
            var materialScan = await _unitOfWork.MaterialScans.SingleOrDefaultAsync(ms => ms.Id == materialScanId);
            if (materialScan == null)
            {
                throw new EntityWithIdNotFoundException<MaterialScan>(materialScanId);
            }
            return materialScan;
        }

        public async Task<IEnumerable<MaterialScan>> ListAllMaterialScansAsync()
        {
            var materialScans = await _unitOfWork.MaterialScans.GetAllAsync();
            return materialScans;
        }

        // TODO: validation
        public async Task<MaterialScan> UpdateMaterialScanAsync(MaterialScan materialScan)
        {
            _unitOfWork.MaterialScans.Update(materialScan);
            materialScan.MaterialSample.RawImages = null;
            await _unitOfWork.CommitAsync();
            return materialScan;
        }
    }
}
