using Core.Enums;
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
    public class MaterialScanServiceTest : ServiceTestBase
    {
        private readonly IMaterialScanService _materialScanService;

        public MaterialScanServiceTest()
        {
            _materialScanService = new MaterialScanService(_unitOfWork);
        }

        [Fact]
        public async void CreateMaterialScanAsync_CreatesMaterialScanWithCorrectProperties()
        {
            var materialSamples = await _unitOfWork.MaterialSamples.GetAllAsync();
            var materialSample = materialSamples.ToList()[^1];
            var oldMaterialScanCount = (await _unitOfWork.MaterialScans.GetAllAsync()).Count();

            var materialScan = new MaterialScan
            {
                MaterialSample = materialSample,
                ScanFiles = new List<ScanFile>
                {
                    new ScanFile
                    {
                        FilePath = "test.dcm"
                    }
                }
            };

            var result = await _materialScanService.CreateMaterialScanAsync(materialScan);
            result.Should().Be(materialScan);
            var newMaterialScanCount = (await _unitOfWork.MaterialScans.GetAllAsync()).Count();
            newMaterialScanCount.Should().Be(oldMaterialScanCount + 1);

            materialSamples = await _unitOfWork.MaterialSamples.GetAllAsync();
            materialSample = materialSamples.ToList()[^1];
            materialSample.MaterialScan.Id.Should().Be(result.Id);
        }

        [Fact]
        public async void GetMaterialScanByIdAsync_ReturnsCorrectMaterialScan()
        {
            var materialScans = await _unitOfWork.MaterialScans.GetAllAsync();
            var materialScan = materialScans.ToList()[^1];

            var result = await _materialScanService.GetMaterialScanByIdAsync(materialScan.Id);
            result.Should().Be(materialScan);
        }

        [Fact]
        public async void GetMaterialScanByIdAsync_ThrowsExceptionWhenIncorrectMaterialScanId()
        {
            var materialScanId = -1;

            Func<Task> act = async () => await _materialScanService.GetMaterialScanByIdAsync(materialScanId);
            await act.Should().ThrowAsync<EntityWithIdNotFoundException<MaterialScan>>();
        }

        [Fact]
        public async void UpdateMaterialScanAsync_UpdatesMaterialScanProperties()
        {
            var materialScans = await _unitOfWork.MaterialScans.GetAllAsync();
            var materialScan = materialScans.ToList()[^1];
            var materialScanId = materialScan.Id;

            var updatedMaterialScanFileType = ScanFileFormat.PNG;
            materialScan.ScanFileFormat = updatedMaterialScanFileType;

            var result = await _materialScanService.UpdateMaterialScanAsync(materialScan);
            result.Id.Should().Be(materialScanId);

            materialScan = await _materialScanService.GetMaterialScanByIdAsync(materialScanId);
            result.ScanFileFormat.Should().Be(materialScan.ScanFileFormat);
        }
    }
}
