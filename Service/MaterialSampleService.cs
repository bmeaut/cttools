using Core.Enums;
using Core.Exceptions;
using Core.Image;
using Core.Interfaces;
using Core.Interfaces.Image;
using Core.Services;
using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class MaterialSampleService : IMaterialSampleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWorkspaceService _workspaceService;

        public MaterialSampleService(
            IUnitOfWork unitOfWork,
            IWorkspaceService workspaceService)
        {
            _unitOfWork = unitOfWork;
            _workspaceService = workspaceService;
        }

        // TODO: validation
        public async Task<MaterialSample> CreateMaterialSampleAsync(MaterialSample materialSample)
        {
            await _unitOfWork.MaterialSamples.AddAsync(materialSample);
            await _unitOfWork.CommitAsync();

            if (materialSample.MaterialScan != null)
            {
                materialSample.RawImages = GetRawImageSourceForMaterialSample(materialSample);
            }

            return materialSample;
        }

        public async Task<IEnumerable<MaterialSample>> ListAllMaterialSamplesAsync()
        {
            var materialSamples = await _unitOfWork.MaterialSamples.GetAllAsync();
            foreach (var materialSample in materialSamples)
            {
                if ((materialSample.RawImages == null) && (materialSample.MaterialScan != null))
                {
                    materialSample.RawImages = GetRawImageSourceForMaterialSample(materialSample);
                }
            }
            return materialSamples;
        }

        public async Task<IEnumerable<MaterialSample>> ListMaterialSamplesByWorkspaceIdAsync(int workspaceId)
        {
            var workspace = await _workspaceService.GetWorkspaceByIdAsync(workspaceId);
            return _unitOfWork.MaterialSamples.Where(ms => ms.WorkspaceId == workspaceId);
        }

        public async Task<MaterialSample> GetMaterialSampleByIdAsync(int materialSampleId)
        {
            var materialSample = await _unitOfWork.MaterialSamples.SingleOrDefaultAsync(ms => ms.Id == materialSampleId);
            if (materialSample == null)
            {
                throw new EntityWithIdNotFoundException<MaterialSample>(materialSampleId);
            }

            if ((materialSample.RawImages == null) && (materialSample.MaterialScan != null))
            {
                materialSample.RawImages = GetRawImageSourceForMaterialSample(materialSample);
            }

            return materialSample;
        }

        // TODO: validation
        public async Task<MaterialSample> UpdateMaterialSampleAsync(MaterialSample materialSample)
        {
            _unitOfWork.MaterialSamples.Update(materialSample);
            await _unitOfWork.CommitAsync();

            if (materialSample.MaterialScan != null)
            {
                materialSample.RawImages = GetRawImageSourceForMaterialSample(materialSample);
            }

            return materialSample;
        }

        public async Task DeleteMaterialSampleAsync(int materialSampleId)
        {
            var materialSample = await _unitOfWork.MaterialSamples.SingleOrDefaultAsync(ms => ms.Id == materialSampleId);
            _unitOfWork.MaterialSamples.Remove(materialSample);
            await _unitOfWork.CommitAsync();
        }

        public async Task<IEnumerable<UserGeneratedFile>> ListUserGeneratedFilesForMaterialSampleAsync(int materialSampleId)
        {
            var materialSample = await GetMaterialSampleByIdAsync(materialSampleId);
            return materialSample.UserGeneratedFiles;
        }

        // TODO: validation
        public async Task<MaterialSample> UpdateUserGeneratedFilesAsync(int materialSampleId, IEnumerable<UserGeneratedFile> files)
        {
            var materialSample = await GetMaterialSampleByIdAsync(materialSampleId);
            materialSample.UserGeneratedFiles = files;
            return await UpdateMaterialSampleAsync(materialSample);
        }

        private IRawImageSource GetRawImageSourceForMaterialSample(MaterialSample materialSample)
        {
            var filePaths = materialSample.MaterialScan.ScanFiles.Select(sf => sf.FilePath).ToArray();
            IRawImageSource rawImageSource = null;
            switch (materialSample.MaterialScan.ScanFileFormat)
            {
                case ScanFileFormat.DICOM:
                    rawImageSource = new DicomReader(filePaths);
                    break;
                case ScanFileFormat.PNG:
                    double xRes = 1;
                    double yRes = 1;
                    double zRes = 1;
                    if (materialSample.RawImages != null)
                    {
                        xRes = materialSample.RawImages.XResolution;
                        yRes = materialSample.RawImages.YResolution;
                        zRes = materialSample.RawImages.ZResolution;
                    }
                    rawImageSource = new PngReader(filePaths);
                    rawImageSource.XResolution = xRes;
                    rawImageSource.YResolution = yRes;
                    rawImageSource.ZResolution = zRes;

                    break;
                default:
                    break;
            }
            rawImageSource.DicomLevel = materialSample.DicomLevel;
            rawImageSource.DicomRange = materialSample.DicomRange;
            return rawImageSource;
        }
    }
}
