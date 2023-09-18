using Core.Image;
using Core.Interfaces.Image;
using Core.Operation;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Core.Test.Operation
{
    public class ThresholdingOperationTests
    {
        private readonly ThresholdingOperation op;
        private readonly ThresholdingOperationProperties properties;
        private readonly OperationContext context;
        private readonly BlobImage blobImage;
        private readonly BlobImageProxy proxy;
        private readonly CancellationToken token = new CancellationToken();

        public ThresholdingOperationTests()
        {
            op = new ThresholdingOperation();
            properties = new ThresholdingOperationProperties();
            context = new OperationContext();
            blobImage = new BlobImage(200, 200);
            proxy = blobImage.GetProxy();

            context.BlobImages = new Dictionary<int, IBlobImage>
            {
                { 0, proxy }
            };
            context.ActiveLayer = 0;
            context.OperationProperties = properties;
        }

        // TODO: get current raw image from the context
        private readonly Mat image = new Mat(200, 200, MatType.CV_8UC1, new Scalar(0));

        #region Thresholding tests
        [Fact]
        public void EmptyImageReturnsNoMasks()
        {
            Assert.Empty(ConnectedComponentsHelper.GetThresholdResultMasks(image, 128, 255, ThresholdTypes.Binary));
        }

        [Fact]
        public void SingleRectImage_DoesNotReturnBackgroundMask()
        {
            Cv2.Rectangle(image, new Rect(50, 50, 50, 50), new Scalar(255), -1);
            var masks = ConnectedComponentsHelper.GetThresholdResultMasks(image, 128, 255, ThresholdTypes.Binary);
            Assert.Single(masks);
        }
        #endregion

        #region Operation Run level tests
        [Fact]
        public async void InvocationViaRun()
        {
            Cv2.Rectangle(image, new Rect(50, 50, 50, 50), new Scalar(255), -1);
            properties.Threshold = 128;
            properties.MaxValue = 255;
            properties.ThresholdType = ThresholdTypes.Binary;

            context.RawImages = new Dictionary<int, Bitmap>();
            context.RawImages.Add(0, OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image));
            await op.Run(context, new Progress<double>(), token);
            Assert.Equal(50 * 50, proxy.GetMask(1).CountNonZero());
            Assert.Equal(2, proxy.GetNextUnusedBlobId());
        }
        #endregion
    }
}
