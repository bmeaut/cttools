using Core.Image;
using Core.Interfaces.Operation;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class ThresholdingOperation : MultiLayerOperationBase
    {
        public override string Name => nameof(ThresholdingOperation);

        public override OperationProperties DefaultOperationProperties => new ThresholdingOperationProperties();

        public override async Task<OperationContext> RunOneLayer(OperationContext context, int layer)
        {
            Mat rawImage = BitmapConverter.ToMat(context.RawImages[layer]);
            return RunOneLayer(context, layer, rawImage);
        }

        public OperationContext RunOneLayer(OperationContext context, int layer, Mat image)
        {
            Mat grayscaleImage = image;
            if (image.Channels() > 1)
                Cv2.CvtColor(image, grayscaleImage, ColorConversionCodes.BGR2GRAY);

            var props = context.OperationProperties as ThresholdingOperationProperties;
            var masks = ConnectedComponentsHelper.GetThresholdResultMasks(image, props.Threshold, props.MaxValue, props.ThresholdType);
            var blobImage = context.BlobImages[layer];

            foreach (var mask in masks)
            {
                var nextAvailableBlobId = blobImage.GetNextUnusedBlobId();
                blobImage.SetBlobMask(mask, nextAvailableBlobId);
                blobImage.SetTag(nextAvailableBlobId, "TresholdingOperation", 44);
            }
            return context;
        }
    }

    public class ThresholdingOperationProperties : MultiLayerOperationProtertiesBase
    {
        public int Threshold { get; set; } = 50;
        public int MaxValue { get; set; } = 255;
        public ThresholdTypes ThresholdType { get; set; } = ThresholdTypes.Binary;
    }

}
