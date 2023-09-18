using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Common.Models.Images;
using Cv4s.Operations.ConnectedComponents3DOperation;
using Cv4s.Operations.MultiChannelThresoldOperation;
using Cv4s.Operations.PoreSizeStatOperation;
using Cv4s.Operations.ReadImageOperation;
using Cv4s.Operations.RoiOperation;
using Cv4s.Operations.SelectMaterialsOperation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cv4s.Measurements
{
    public class TestBlobImageMeasurement : IMeasurement
    {
        private readonly ReadImageOperation _readImageOperation;
        private readonly MultiChannelThresholdOperation _multiChannelThresholdOperation;
        private readonly RoiOperation _roiOperation;
        private readonly SelectMaterialsOperation _selectMaterialsOperation;
        private readonly ConnectedComponents3DOperation _connectedComponentsOperation;
        private readonly PoreSizeStatOperation _poreSizeStatOperation;
        private IRawImageSource _imageSource;
        private IBlobImageSource _blobImageSource;

        public TestBlobImageMeasurement(ReadImageOperation readImageOperation,
                                        MultiChannelThresholdOperation multiChannelThresholdOperation,
                                        RoiOperation roiOperation,
                                        SelectMaterialsOperation selectMaterialsOperation,
                                        ConnectedComponents3DOperation connectedComponentsOperation,
                                        PoreSizeStatOperation poreSizeStatOperation)
        {
            _readImageOperation = readImageOperation;
            _multiChannelThresholdOperation = multiChannelThresholdOperation;
            _roiOperation = roiOperation;
            _selectMaterialsOperation = selectMaterialsOperation;
            _connectedComponentsOperation = connectedComponentsOperation;
            _poreSizeStatOperation = poreSizeStatOperation;
        }

        public async Task RunAsync()
        {
            try
            {
                Console.WriteLine($"Starting {nameof(TestBlobImageMeasurement)} measurement...");

                _imageSource = _readImageOperation.ReadFiles();
                _blobImageSource = new BlobImageSource(_imageSource);

                _blobImageSource = await _multiChannelThresholdOperation.ThresholdImagesAsync(_imageSource, _blobImageSource);
                _blobImageSource = _roiOperation.CalcRoi(_imageSource, _blobImageSource);//Hátrány, hogy ahányszor elakarjuk végezni a műveletet annyiszor kell újrahíni a kódban.
                _blobImageSource = _selectMaterialsOperation.SelectMaterial(_imageSource, _blobImageSource);
                _blobImageSource = _connectedComponentsOperation.ConnectComponents(_imageSource, _blobImageSource);
                _poreSizeStatOperation.CalcPoreSizeStat(_imageSource, _blobImageSource);
                Console.WriteLine("Finishing measurement...");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
