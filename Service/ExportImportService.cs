using AutoMapper;
using Core.Exceptions;
using Core.Interfaces;
using Core.Operation;
using Core.Services;
using Core.Services.Dto;
using Core.Services.Dto.Json;
using Core.Workspaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class ExportImportService : IExportImportService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWorkspaceService _workspaceService;
        private readonly IMaterialSampleService _materialSampleService;
        private readonly IMaterialScanService _materialScanService;
        private readonly IMeasurementService _measurementService;
        private readonly IOperationService _operationService;
        private readonly JsonSerializerSettings settings;

        private readonly string workspaceJsonName = "workspace.json";

        public static string StaticExtractDirectoryPath => Path.Join(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CV4S");

        public string ExtractDirectoryPath => ExportImportService.StaticExtractDirectoryPath;

        public ExportImportService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IWorkspaceService workspaceService,
            IMaterialSampleService materialSampleService,
            IMaterialScanService materialScanService,
            IMeasurementService measurementService,
            IOperationService operationService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _workspaceService = workspaceService;
            _materialSampleService = materialSampleService;
            _materialScanService = materialScanService;
            _measurementService = measurementService;
            _operationService = operationService;
            settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
            };
        }

        public async Task<byte[]> ExportWorkspaceAsync(int workspaceId)
        {
            var workspace = await _workspaceService.GetWorkspaceByIdWithAllEntitiesAsync(workspaceId);
            var workspaceJson = _mapper.Map<WorkspaceJson>(workspace);
            var workspaceString = JsonConvert.SerializeObject(workspaceJson, settings);

            var inMemoryFiles = new List<InMemoryFile>();
            foreach (var materialSample in workspaceJson.MaterialSamples)
            {
                if (materialSample.MaterialScan != null)
                {
                    foreach (var file in materialSample.MaterialScan.ScanFiles)
                    {
                        inMemoryFiles.Add(new InMemoryFile
                        {
                            FileName = GetZipScanFilePath(workspaceJson, materialSample, Path.GetFileName(file.FilePath)),
                            Content = File.ReadAllBytes(file.FilePath)
                        });
                    }
                }
                if (materialSample.UserGeneratedFiles != null)
                {
                    foreach (var file in materialSample.UserGeneratedFiles)
                    {
                        inMemoryFiles.Add(new InMemoryFile
                        {
                            FileName = GetZipScanFilePath(workspaceJson, materialSample, Path.GetFileName(file.Path)),
                            Content = File.ReadAllBytes(file.Path)
                        });
                    }
                }
            }

            inMemoryFiles.Add(new InMemoryFile
            {
                FileName = workspaceJsonName,
                Content = Encoding.Default.GetBytes(workspaceString)
            });

            return GetZipArchive(inMemoryFiles);
        }

        public async Task ImportWorkspaceAsync(byte[] file)
        {
            WorkspaceJson workspace = ReadZipArchive(file);
            var newWorkspace = await CreateWorkspaceAsync(workspace);

            // MaterialSamples
            foreach (MaterialSampleJson materialSample in workspace.MaterialSamples)
            {
                var createdMaterialSample = await CreateMaterialSampleAsync(materialSample, newWorkspace);
                await CreateMaterialScanAsync(materialSample, createdMaterialSample);
                await CreateMeasurementAsync(createdMaterialSample.Id, materialSample.Measurements);
            }
        }

        private async Task<MaterialSample> CreateMaterialSampleAsync(MaterialSampleJson materialSampleJson, Workspace newWorkspace)
        {
            var ms = new MaterialSample
            {
                WorkspaceId = newWorkspace.Id,
                Label = materialSampleJson.Label
            };

            var createdMaterialSample = await _materialSampleService.CreateMaterialSampleAsync(ms);
            await CreateUserGeneratedFilesAsync(materialSampleJson.UserGeneratedFiles, createdMaterialSample);
            return createdMaterialSample;
        }

        private async Task CreateUserGeneratedFilesAsync(IEnumerable<UserGeneratedFileJson> userGeneratedFiles, MaterialSample createdMaterialSample)
        {
            var userGeneratedFileCollection = new List<UserGeneratedFile>();
            foreach (var ugf in userGeneratedFiles)
                userGeneratedFileCollection.Add(new UserGeneratedFile
                {
                    MaterialSampleId = createdMaterialSample.Id,
                    Name = ugf.Name,
                    Path = ugf.Path,
                });
            createdMaterialSample.UserGeneratedFiles = userGeneratedFileCollection;
            await _materialSampleService.UpdateMaterialSampleAsync(createdMaterialSample);
        }

        private async Task CreateMaterialScanAsync(MaterialSampleJson materialSampleJson, MaterialSample createdMaterialSample)
        {
            var materialScanEntity = new MaterialScan
            {
                MaterialSample = createdMaterialSample,
                MaterialSampleId = createdMaterialSample.Id,
                ScanFileFormat = materialSampleJson.MaterialScan.ScanFileFormat
            };
            var scanFileCollection = new List<ScanFile>();
            foreach (var sf in materialSampleJson.MaterialScan.ScanFiles)
                scanFileCollection.Add(new ScanFile { FilePath = sf.FilePath });
            materialScanEntity.ScanFiles = scanFileCollection;
            await _materialScanService.CreateMaterialScanAsync(materialScanEntity);
        }

        private async Task CreateMeasurementAsync(int id, IEnumerable<MeasurementJson> measurements)
        {
            foreach (var measurement in measurements)
            {
                var measurementEntity = new Measurement
                {
                    Name = measurement.Name,
                    Description = measurement.Description
                };
                measurementEntity = await _measurementService.CreateMeasurementAsync(id, measurementEntity);
                measurementEntity.BlobImageEntities = measurement.BlobImageEntities;
                await _measurementService.UpdateMeasurementAsync(measurementEntity);
                // Operations
                foreach (OperationContextJson operationContextJson in measurement.OperationContexts)
                {
                    var operationContext = new OperationContext
                    {
                        OperationName = operationContextJson.OperationName,
                        OperationProperties = operationContextJson.OperationProperties,
                        OperationRunEventArgs = null,
                        MeasurementId = measurementEntity.Id,
                        ActiveLayer = operationContextJson.ActiveLayer,
                        BlobImages = null,
                        RawImages = null,
                        RawImageMetadata = null,
                        InternalOutputs = operationContextJson.InternalOutputs
                    };
                    await _operationService.AddOperationContextAsync(operationContext);
                }
            }
            await _unitOfWork.CommitAsync();
        }

        private async Task<Workspace> CreateWorkspaceAsync(WorkspaceJson workspace)
        {
            var workspaceEntity = new Workspace
            {
                Name = workspace.Name,
                Description = workspace.Description,
                Customer = workspace.Customer,
                DueDate = workspace.DueDate,
                DayOfArrival = workspace.DayOfArrival,
                Price = workspace.Price
            };
            return await _workspaceService.CreateWorkspaceAsync(workspaceEntity);
        }

        private class InMemoryFile
        {
            public string FileName { get; set; }

            public byte[] Content { get; set; }
        }

        private byte[] GetZipArchive(List<InMemoryFile> files)
        {
            byte[] archiveFile;
            using (var archiveStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        var zipArchiveEntry = archive.CreateEntry(file.FileName, CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open())
                            zipStream.Write(file.Content, 0, file.Content.Length);
                    }
                }
                archiveFile = archiveStream.ToArray();
            }
            return archiveFile;
        }

        private WorkspaceJson ReadZipArchive(byte[] bytes)
        {
            WorkspaceJson workspace = null;
            using (var archiveStream = new MemoryStream(bytes))
            {
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, true))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith(workspaceJsonName, StringComparison.OrdinalIgnoreCase))
                        {
                            using var stream = entry.Open();
                            using StreamReader sr = new StreamReader(stream);
                            workspace = JsonConvert.DeserializeObject<WorkspaceJson>(sr.ReadToEnd(), settings);
                        }
                        else
                        {
                            ExtractEntry(entry);
                        }
                    }
                }
            }
            // Set URL links in ScanFileJson objects to point to the files that we just extracted
            foreach (var materialSample in workspace.MaterialSamples)
            {
                foreach (var scanFileJson in materialSample.MaterialScan.ScanFiles)
                {
                    var name = GetZipScanFilePath(workspace, materialSample, Path.GetFileName(scanFileJson.FilePath));
                    scanFileJson.FilePath = GetNewScanFilePath(name);
                }
                foreach (var userGeneratedFileJson in materialSample.UserGeneratedFiles)
                {
                    var name = GetZipScanFilePath(workspace, materialSample, Path.GetFileName(userGeneratedFileJson.Path));
                    userGeneratedFileJson.Path = GetNewScanFilePath(name);
                }
            }
            return workspace;
        }

        private void ExtractEntry(ZipArchiveEntry entry)
        {
            string destinationPath = GetNewScanFilePath(entry.FullName);
            Directory.CreateDirectory(Directory.GetParent(destinationPath).FullName);
            try
            {
                entry.ExtractToFile(destinationPath);
            }
            catch (IOException)
            {
                throw new WorkspaceImportFileExistsException(destinationPath);
            }
        }

        private string GetNewScanFilePath(string name) => Path.GetFullPath(Path.Combine(ExtractDirectoryPath, name));

        private string GetZipScanFilePath(WorkspaceJson workspaceJson, MaterialSampleJson materialSample, string fileName)
        {
            return Path.Combine(workspaceJson.Name,
                                materialSample.Label,
                                materialSample.MaterialScan.Id.ToString(),
                                fileName
                                );
        }

        private bool IsImageResourceFile(string entry)
        {
            return entry.EndsWith(".dcm", StringComparison.OrdinalIgnoreCase) ||
                    entry.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }
    }
}
