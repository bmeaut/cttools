using Core.Image;
using Core.Interfaces.Image;
using Core.Operation;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Core.Test.Operation
{
    public class MultiChannelThresholdingOperationTests
    {
        private readonly MultiChannelThresholdingOperation op;
        private readonly MultiChannelThresholdingOperationProperties properties;
        private readonly OperationContext context;
        private readonly BlobImage blobImage;
        private readonly List<int> tresholValues;
        private readonly int imageWidth = 200;
        private readonly int imageHeight = 200;

        public MultiChannelThresholdingOperationTests()
        {
            op = new MultiChannelThresholdingOperation();
            tresholValues = new List<int>() { 50, 150 };
            properties = new MultiChannelThresholdingOperationProperties
            {
                TresholdValues = tresholValues
            };
            context = new OperationContext();
            blobImage = new BlobImage(imageWidth, imageHeight);

            context.BlobImages = new Dictionary<int, IBlobImage>
            {
                { 0, blobImage }
            };
            context.ActiveLayer = 0;
            context.OperationProperties = properties;

            context.RawImages = new Dictionary<int, Bitmap>()
            {
                { 0, null }
            };
        }

        private readonly Rect[] Component1Rects = new Rect[]
        {
            new Rect(0, 0, 10, 10),
            new Rect(50, 50, 10, 10),
        };

        private readonly Rect[] Component2Rects = new Rect[]
        {
            new Rect(0, 100, 20, 30),
            new Rect(100, 0, 20, 30),
        };

        [Fact]
        public async void ImageWith3Components_AppliesCorrectTagValues()
        {
            var testImage = Get3ComponentsImage();
            context.RawImages[0] = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(testImage);

            var resultContext = await op.Run(context, new Progress<double>(), new System.Threading.CancellationToken());
            var resultImage = resultContext.BlobImages[0];
            var components0Blobs = resultImage.GetBlobsByTagValue(Tags.ComponentId.ToString(), 0);
            var components1Blobs = resultImage.GetBlobsByTagValue(Tags.ComponentId.ToString(), 1);
            var components2Blobs = resultImage.GetBlobsByTagValue(Tags.ComponentId.ToString(), 2);
            Assert.Single(components0Blobs);
            Assert.Equal(2, components1Blobs.Count());
            Assert.Equal(2, components2Blobs.Count());

            for (int x = 0; x < imageWidth; x++)
            {
                for (int y = 0; y < imageHeight; y++)
                {
                    int blobId = resultImage[y, x];

                    if (components0Blobs.Contains(blobId))
                    {
                        Assert.False(CointainedByComponentRects(x, y, Component1Rects));
                        Assert.False(CointainedByComponentRects(x, y, Component2Rects));
                    }
                    if (components1Blobs.Contains(blobId))
                    {
                        Assert.True(CointainedByComponentRects(x, y, Component1Rects));
                    }
                    if (components2Blobs.Contains(blobId))
                    {
                        Assert.True(CointainedByComponentRects(x, y, Component2Rects));
                    }
                }
            }
        }

        private bool CointainedByComponentRects(int x, int y, Rect[] componentRects)
        {
            return componentRects.Any(r => r.Contains(x, y));
        }

        private Mat Get3ComponentsImage()
        {
            var testImage = new Mat(imageHeight, imageWidth, MatType.CV_8UC1, new Scalar(0));
            foreach (var rect in Component1Rects)
            {
                Cv2.Rectangle(testImage, rect, tresholValues[0] + 1, -1);
            }
            foreach (var rect in Component2Rects)
            {
                Cv2.Rectangle(testImage, rect, tresholValues[1] + 1, -1);
            }
            return testImage;
        }
    }
}
