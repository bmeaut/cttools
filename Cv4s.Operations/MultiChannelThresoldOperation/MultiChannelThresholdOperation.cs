using Cv4s.Common.Enums;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Operations.OperationTools;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Cv4s.Operations.MultiChannelThresoldOperation
{
    public class MultiChannelThresholdOperation : OperationBase
    {
        private readonly IUIHandlerService _uIHandlerService;

        private int[] _valueToComponentIdMapper;
        private int _componentCount;

        public MultiChannelThresholdOperation(IUIHandlerService uIHandlerService)
        {
            _uIHandlerService = uIHandlerService;

            Properties = new MultiChannelThresholdOperationProperties();

            RunEventArgs.IsCallableFromCanvas = false;
            RunEventArgs.IsCallableFromButton = true;
        }

        public async Task<IBlobImageSource> ThresholdImagesAsync(IRawImageSource rawImages, IBlobImageSource refBlobImages)
        {
            _uIHandlerService.ShowMeasurementEditor(this, rawImages, refBlobImages);

            var props = Properties as MultiChannelThresholdOperationProperties;

            var thresholdValues = props!.TresholdValues;

            thresholdValues.RemoveAll(t => t >= 255 || t <= 0);

            _valueToComponentIdMapper = GetValueToComponentIdMapperArray(thresholdValues);
            thresholdValues.Sort(); //kell ez ? TODO

            _componentCount = thresholdValues.Count + 1;

            Parallel.ForEach(refBlobImages.Keys, layerID =>
            {
                RunOneLayer(rawImages, refBlobImages, layerID);
            });

            return refBlobImages; 
        }

        public void RunOneLayer(IRawImageSource RawImages, IBlobImageSource BlobImages, int currentLayer)
        {
            Mat image = BitmapConverter.ToMat(RawImages[currentLayer]);
            Mat grayscaleImage = image;
            if (image.Channels() > 1)
                Cv2.CvtColor(image, grayscaleImage, ColorConversionCodes.BGR2GRAY);

            var componentMasks = GetComponentMasks(image, _valueToComponentIdMapper, _componentCount);
            var blobImage = BlobImages[currentLayer];

            SegmentBlobs(blobImage, componentMasks);
        }


        private int[] GetValueToComponentIdMapperArray(List<int> thresholdValues)
        {
            var valueToComponentIdMapper = new int[256];
            int componentId = 0;
            int tresholdValuesIndex = 0;
            for (int value = 0; value < 256; value++)
            {
                if (value == thresholdValues[tresholdValuesIndex])
                {
                    componentId++;
                    if (tresholdValuesIndex < thresholdValues.Count - 1)
                        tresholdValuesIndex++;
                }
                valueToComponentIdMapper[value] = componentId;
            }
            return valueToComponentIdMapper;
        }

        private Mat[] GetComponentMasks(Mat image, int[] mapper, int componentCount)
        {
            var componentsMasks = new Mat[componentCount];
            var componentMaskIndexers = new Mat.Indexer<byte>[componentCount];
            for (int i = 0; i < componentsMasks.Length; i++)
            {
                componentsMasks[i] = new Mat(image.Size(), MatType.CV_8UC1, 0);
                componentMaskIndexers[i] = componentsMasks[i].GetGenericIndexer<byte>();
            }

            var indexer = image.GetGenericIndexer<byte>();
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    int componentId = mapper[indexer[y, x]];
                    componentMaskIndexers[componentId][y, x] = 255;
                }
            }
            return componentsMasks;
        }

        private void SegmentBlobs(IBlobImage blobImage, Mat[] componentMasks)
        {
            for (int componentId = 0; componentId < componentMasks.Length; componentId++)
            {
                var blobs = ConnectedComponentsHelper.SegmentMask(blobImage, componentMasks[componentId]);
                foreach (var blob in blobs)
                {
                    blobImage.SetTag(blob, Tags.ComponentId.ToString(), componentId);
                }
            }
        }
    }
}
