using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class MultiChannelThresholdingOperation : MultiLayerParallelOperationBase
    {
        private int[] _valueToComponentIdMapper;
        private int _componentCount;

        public override string Name => "Multi-channel Thresholding";

        public override OperationProperties DefaultOperationProperties => new MultiChannelThresholdingOperationProperties();

        public override async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var props = context.OperationProperties as MultiChannelThresholdingOperationProperties;
            var thresholdValues = props.TresholdValues;
            thresholdValues.RemoveAll(t => t >= 255 || t <= 0);
            _valueToComponentIdMapper = GetValueToComponentIdMapperArray(thresholdValues);
            thresholdValues.Sort();
            _componentCount = GetComponentCount(thresholdValues);

            return await base.Run(context, progress, token);
        }

        public override async Task<OperationContext> RunOneLayer(OperationContext context, int layer, IProgress<double> progress, CancellationToken token)
        {
            Mat image = BitmapConverter.ToMat(context.RawImages[layer]);
            Mat grayscaleImage = image;
            if (image.Channels() > 1)
                Cv2.CvtColor(image, grayscaleImage, ColorConversionCodes.BGR2GRAY);

            var componentMasks = GetComponentMasks(image, _valueToComponentIdMapper, _componentCount);
            var blobImage = context.BlobImages[layer];

            SegmentBlobs(blobImage, componentMasks);

            return context;
        }

        public static void SegmentBlobs(IBlobImage blobImage, Mat[] componentMasks)
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

        private int GetComponentCount(List<int> thresholdValues) => thresholdValues.Count + 1;

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
    }
    public class MultiChannelThresholdingOperationProperties : MultiLayerOperationProtertiesBase
    {
        public List<int> TresholdValues { get; set; } = new List<int>() { 50, 150 };
    }
}
