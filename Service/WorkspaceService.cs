using Core.Exceptions;
using Core.Interfaces;
using Core.Services;
using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WorkspaceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // TODO: validation
        public async Task<Workspace> CreateWorkspaceAsync(Workspace workspace)
        {
            await _unitOfWork.Workspaces.AddAsync(workspace);
            await _unitOfWork.CommitAsync();

            return workspace;
        }

        public async Task<Workspace> GetWorkspaceByIdAsync(int workspaceId)
        {
            var workspace = await _unitOfWork.Workspaces.SingleOrDefaultAsync(w => w.Id == workspaceId);
            if (workspace == null)
            {
                throw new EntityWithIdNotFoundException<Workspace>(workspaceId);
            }

            return workspace;
        }

        public async Task<Workspace> GetWorkspaceByIdWithAllEntitiesAsync(int workspaceId)
        {
            var workspace = await _unitOfWork.Workspaces.GetByIdWithAllEntitiesAsync(workspaceId);
            if (workspace == null)
            {
                throw new EntityWithIdNotFoundException<Workspace>(workspaceId);
            }

            return workspace;
        }

        public async Task<IEnumerable<Workspace>> ListWorkspacesAsync()
        {
            return await _unitOfWork.Workspaces.GetAllAsync();
        }

        // TODO: validation
        public async Task<Workspace> UpdateWorkspaceAsync(Workspace workspace)
        {
            _unitOfWork.Workspaces.Update(workspace);
            await _unitOfWork.CommitAsync();
            return workspace;
        }

        public async Task DeleteWorkspaceAsync(int workspaceId)
        {
            // Entity framework needs the objects to be in context for delete
            var workspace = await GetWorkspaceByIdWithAllEntitiesAsync(workspaceId);
            //var workspace = await GetWorkspaceByIdAsync(workspaceId);
            // EF cannot resolve sessionContexts automatically because of a circular dependeny
            RemoveAllSessions(workspace);
            DeleteAssociatedFiles(workspace);

            //var workspace = await _unitOfWork.Workspaces.SingleOrDefaultAsync(w => w.Id == workspaceId);
            _unitOfWork.Workspaces.Remove(workspace);
            await _unitOfWork.CommitAsync();
        }

        private void RemoveAllSessions(Workspace workspace)
        {
            foreach (var materialSample in workspace.MaterialSamples)
            {
                var sessionContexts = _unitOfWork.SessionContexts.Where(s => s.CurrentMeasurement.MaterialSampleId == materialSample.Id);
                _unitOfWork.SessionContexts.RemoveRange(sessionContexts);
            }
            _unitOfWork.CommitAsync();
        }

        private void DeleteAssociatedFiles(Workspace workspace)
        {
            foreach (var materialSample in workspace.MaterialSamples)
            {
                foreach (var scanFile in materialSample.MaterialScan.ScanFiles)
                    DeleteFile(scanFile.FilePath);
                foreach (var ugf in materialSample.UserGeneratedFiles)
                    DeleteFile(ugf.Path);
            }
        }
        private void DeleteFile(string filePath)
        {
            // Deleting only the actual files in the inner Appdata folder only.
            if (filePath.Contains(ExportImportService.StaticExtractDirectoryPath))
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }
    }
}
