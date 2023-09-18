using Core.Exceptions;
using Core.Services;
using Core.Workspaces;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Service.Test
{
    public class MaterialSampleServiceTest : ServiceTestBase
    {
        private readonly IMaterialSampleService _materialSampleService;

        public MaterialSampleServiceTest() : base()
        {
            var workspaceService = new WorkspaceService(_unitOfWork);
            _materialSampleService = new MaterialSampleService(_unitOfWork, workspaceService);
        }

        [Fact]
        public async void CreateMaterialSampleAsync_CreatesMaterialSampleWithCorrectProperties()
        {
            var workspaces = await _unitOfWork.Workspaces.GetAllAsync();
            var workspace = workspaces.ToList()[^1];
            var oldMaterialSampleCount = (await _unitOfWork.MaterialSamples.GetAllAsync()).Count();

            var materialSample = new MaterialSample
            {
                Label = "Test material sample",
                WorkspaceId = workspace.Id
            };

            var result = await _materialSampleService.CreateMaterialSampleAsync(materialSample);
            result.Should().Be(materialSample);
            var newMaterialSampleCount = (await _unitOfWork.MaterialSamples.GetAllAsync()).Count();
        }

        [Fact]
        public async void GetMaterialSampleByIdAsync_ReturnsCorrectMaterialSample()
        {
            var materialSamples = await _unitOfWork.MaterialSamples.GetAllAsync();
            var materialSample = materialSamples.ToList()[^1];

            var result = await _materialSampleService.GetMaterialSampleByIdAsync(materialSample.Id);
            result.Should().Be(materialSample);
        }

        [Fact]
        public async void GetMaterialSampleByIdAsync_ThrowsExceptionWhenIncorrectMaterialSampleId()
        {
            var materialSampleId = -1;

            Func<Task> act = async () => await _materialSampleService.GetMaterialSampleByIdAsync(materialSampleId);
            await act.Should().ThrowAsync<EntityWithIdNotFoundException<MaterialSample>>();
        }

        [Fact]
        public async void UpdateMaterialSampleAsync_UpdatesMaterialSampleProperties()
        {
            var materialSamples = await _unitOfWork.MaterialSamples.GetAllAsync();
            var materialSample = materialSamples.ToList()[^1];
            var materialSampleId = materialSample.Id;

            var updatedMaterialSampleLabel = "Updated material sample";
            materialSample.Label = updatedMaterialSampleLabel;

            var result = await _materialSampleService.UpdateMaterialSampleAsync(materialSample);
            result.Id.Should().Be(materialSampleId);

            materialSample = await _materialSampleService.GetMaterialSampleByIdAsync(materialSampleId);
            result.Label.Should().Be(materialSample.Label);
        }

        [Fact]
        public async void UpdateUserGeneratedFilesAsync_AddsNewUserGeneratedFiles()
        {
            var materialSamples = await _unitOfWork.MaterialSamples.GetAllAsync();
            var materialSample = materialSamples.ToList()[^1];

            var userGeneratedFiles = new List<UserGeneratedFile>
            {
                new UserGeneratedFile
                {
                    Name = "test1",
                    Path = "C:\\test1.png"
                },
                new UserGeneratedFile
                {
                    Name = "test2",
                    Path = "C:\\test2.png"
                }
            };

            var result = await _materialSampleService.UpdateUserGeneratedFilesAsync(materialSample.Id, userGeneratedFiles);
            result.Id.Should().Be(materialSample.Id);

            materialSamples = await _unitOfWork.MaterialSamples.GetAllAsync();
            materialSample = materialSamples.ToList()[^1];
            materialSample.UserGeneratedFiles.Should().HaveCount(2);

            materialSample.UserGeneratedFiles
                .SingleOrDefault(ms => ms.Name == userGeneratedFiles[0].Name && ms.Path == userGeneratedFiles[0].Path);
            materialSample.UserGeneratedFiles
                .SingleOrDefault(ms => ms.Name == userGeneratedFiles[1].Name && ms.Path == userGeneratedFiles[1].Path);
        }

        [Fact]
        public async void UpdateUserGeneratedFilesAsync_UpdatesUserGeneratedFiles()
        {
            var materialSamples = await _unitOfWork.MaterialSamples.GetAllAsync();
            var materialSample = materialSamples.ToList()[^1];

            var userGeneratedFiles = new List<UserGeneratedFile>
            {
                new UserGeneratedFile
                {
                    Name = "test1",
                    Path = "C:\\test1.png"
                },
                new UserGeneratedFile
                {
                    Name = "test2",
                    Path = "C:\\test2.png"
                }
            };
            await _materialSampleService.UpdateUserGeneratedFilesAsync(materialSample.Id, userGeneratedFiles);

            var newUserGeneratedFiles = new List<UserGeneratedFile>
            {
                new UserGeneratedFile
                {
                    Name = "test2",
                    Path = "C:\\test2.jpg"
                },
                new UserGeneratedFile
                {
                    Name = "test3",
                    Path = "C:\\test3.jpg"
                }
            };
            await _materialSampleService.UpdateUserGeneratedFilesAsync(materialSample.Id, userGeneratedFiles);

            materialSamples = await _unitOfWork.MaterialSamples.GetAllAsync();
            materialSample = materialSamples.ToList()[^1];
            materialSample.UserGeneratedFiles.Should().HaveCount(2);

            materialSample.UserGeneratedFiles
                .SingleOrDefault(ms => ms.Name == newUserGeneratedFiles[0].Name && ms.Path == newUserGeneratedFiles[0].Path);
            materialSample.UserGeneratedFiles
                .SingleOrDefault(ms => ms.Name == newUserGeneratedFiles[1].Name && ms.Path == newUserGeneratedFiles[1].Path);
        }
    }
}
