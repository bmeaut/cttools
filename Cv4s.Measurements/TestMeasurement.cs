using Cv4s.Common.Exceptions;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Common.Models.Images;
using Cv4s.Operations.ReadImageOperation;
using Cv4s.Operations.RoiOperation;

namespace Cv4s.Measurements
{
    public class TestMeasurement : IMeasurement
    {
        private readonly MedianBlurOperation _medianBlurOperation;
        private readonly ReadImageOperation _readImageOperation;

        private IRawImageSource _imageSource;
        private IBlobImageSource _blobImageSource;

        public TestMeasurement(MedianBlurOperation roiOperation, ReadImageOperation readImageOperation)
        {
            _medianBlurOperation = roiOperation;
            _readImageOperation = readImageOperation;
        }

        public async Task RunAsync()
        {
            try
            {
                Console.WriteLine("Starting measurement ...");

                _imageSource = _readImageOperation.ReadFiles();

                _blobImageSource = new BlobImageSource(_imageSource);

                await _medianBlurOperation.DegradeImages(_imageSource, _blobImageSource);

                Console.WriteLine("Finishing measurement...");
            }
            catch (MeasurementCancelledException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}