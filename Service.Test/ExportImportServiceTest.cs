using AutoMapper;
using Core.Services;
using Core.Workspaces;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Service.Test
{
    public class ExportImportServiceTest : ServiceTestBase
    {
        private ExportImportService _exportImportService;
        private WorkspaceService _workspaceService;
        private MaterialSampleService _materialSampleService;
        private readonly string dataDirectory;

        public ExportImportServiceTest() : base()
        {
            SetupServices();
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            dataDirectory = Path.Join(projectDirectory, @"\data");
        }

        private void SetupServices()
        {
            IMapper _mapper = new Mapper(DtoMapperConfiguration.Configuration); ;
            _workspaceService = new WorkspaceService(_unitOfWork);
            _materialSampleService = new MaterialSampleService(_unitOfWork, _workspaceService);
            IMaterialScanService _materialScanService = new MaterialScanService(_unitOfWork);
            IOperationService _operationService = new OperationService(_unitOfWork);
            ISessionService _sessionService = new SessionService(_unitOfWork, _workspaceService);
            IHistoryService _historyService = new HistoryService(_mapper);
            IMeasurementService _measurementService = new MeasurementService(
                _unitOfWork,
                _materialSampleService,
                _operationService,
                _sessionService,
                _historyService);

            _exportImportService = new ExportImportService(
                _mapper,
                _unitOfWork,
                _workspaceService,
                _materialSampleService,
                _materialScanService,
                _measurementService,
                _operationService
                );
        }

        [Fact]
        public async void TestExport()
        {
            var workspace = new Workspace
            {
                Name = "New test workspace",
                Description = "Testing a new workspace"
            };

            var result = await _workspaceService.CreateWorkspaceAsync(workspace);
            var file = await _exportImportService.ExportWorkspaceAsync(result.Id);
            file.Should().BeOfType(typeof(byte[]));
        }

        [Fact]
        public async void TestImport()
        {
            var oldWorkspaces = await _workspaceService.ListWorkspacesAsync();
            var oldWorkspaceCount = oldWorkspaces.ToList().Count;

            var exportFileName = "workspace_demo_export.zip";
            var filePath = Path.Join(dataDirectory, exportFileName);
            var file = File.ReadAllBytes(filePath);
            await _exportImportService.ImportWorkspaceAsync(file);

            var workspaces = await _workspaceService.ListWorkspacesAsync();
            var newWorkspaceCount = workspaces.ToList().Count;
            newWorkspaceCount.Should().Be(oldWorkspaceCount + 1);

            // Clean up the extracted files
            string extractedFilesDirectory = Path.Join(_exportImportService.ExtractDirectoryPath, workspaces.ToList().Last().Name);
            Directory.Delete(extractedFilesDirectory, true);
        }
        private void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

    }
}
