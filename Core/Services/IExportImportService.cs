using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IExportImportService
    {
        public Task<byte[]> ExportWorkspaceAsync(int workspaceId);

        public Task ImportWorkspaceAsync(byte[] file);
    }
}
