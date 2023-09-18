using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Image;
using Core.Interfaces.Image;
using Core.Workspaces;

namespace Core.Services
{
    public interface IMaterialSampleService
    {
        public Task<MaterialSample> CreateMaterialSampleAsync(MaterialSample materialSample);

        public Task<IEnumerable<MaterialSample>> ListAllMaterialSamplesAsync();

        public Task<IEnumerable<MaterialSample>> ListMaterialSamplesByWorkspaceIdAsync(int workspaceId);

        public Task<MaterialSample> GetMaterialSampleByIdAsync(int materialSampleId);

        public Task<MaterialSample> UpdateMaterialSampleAsync(MaterialSample materialSample);

        public Task DeleteMaterialSampleAsync(int materialSampleId);

        public Task<IEnumerable<UserGeneratedFile>> ListUserGeneratedFilesForMaterialSampleAsync(int materialSampleId);

        public Task<MaterialSample> UpdateUserGeneratedFilesAsync(int materialSampleId, IEnumerable<UserGeneratedFile> files);
    }
}
