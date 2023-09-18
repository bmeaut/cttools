using Core.Image;
using Core.Interfaces.Image;
using Core.Operation;
using Core.Services;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Core.Test.Operation
{
    public class ManualEditOperationTests
    {
        private readonly ManualEditOperation meo;
        private readonly ManualEditOperationProperties properties;
        private readonly OperationContext context;
        private readonly BlobImage blobImage;
        private readonly BlobImageProxy proxy;
        private readonly CancellationToken token = new CancellationToken();

        public ManualEditOperationTests()
        {
            meo = new ManualEditOperation();
            properties = new ManualEditOperationProperties();
            context = new OperationContext();
            blobImage = new BlobImage(200, 200);
            proxy = blobImage.GetProxy();

            context.BlobImages = new Dictionary<int, IBlobImage>
            {
                { 0, proxy }
            };
            context.ActiveLayer = 0;
            context.OperationProperties = properties;
            context.OperationRunEventArgs = new OperationRunEventArgs();
        }

        [Fact]
        public async void SlicingWithoutStrokesIsIdentity()
        {
            properties.Mode = ManualEditOperationProperties.ModeEnum.Slice;
            await meo.Run(context, new Progress<double>(), token);
            var memento = proxy.ApplyToOriginal();
            Assert.True(memento.IsIdentity);
        }

        private Mat GetFilledMask()
        {
            Mat mask = blobImage.GetEmptyMask();
            mask.SetTo(new Scalar(1));
            return mask;
        }

        [Fact]
        public async Task SlicingWithMultipleStrokesAsync()
        {
            var filledMask = GetFilledMask();
            blobImage.SetBlobMask(filledMask, 1);

            List<Point[]> controlPointsList = new List<Point[]>();
            const int shapeCount = 5;
            for (int idx = 0; idx < shapeCount; idx++)
                controlPointsList.Add(GetUShape(10 + idx * 20));
            context.OperationRunEventArgs.Strokes = controlPointsList.ToArray();

            properties.Mode = ManualEditOperationProperties.ModeEnum.Slice;
            await meo.Run(context, new Progress<double>(), token);

            for (int idx = 0; idx < shapeCount; idx++)
                AssertUShapeInProxy(10 + idx * 20, 0);
            var pixelNumber = filledMask.CountNonZero();
            Assert.Equal(pixelNumber - shapeCount * UShapeStrokeArea, proxy.GetMask(1).CountNonZero());
        }

        [Fact]
        public async Task SlicingAssignsNewBlobIdsIfSeparationiIsAchievedAsync()
        {
            // Blob 1 will be sliced, blob 2 should not be affected
            blobImage.DrawBlobRect(50, 50, 100, 50, blobId: 1);
            blobImage.DrawBlobRect(50, 150, 100, 50, blobId: 2);
            context.OperationRunEventArgs.Strokes
                = new Point[][] { new Point[] {
                    new Point(100,40), new Point(100,110) } };
            properties.Mode = ManualEditOperationProperties.ModeEnum.Slice;
            await meo.Run(context, new Progress<double>(), token);

            Assert.True(proxy[50, 100] == 0);
            Assert.True(proxy[50, 50] != 1);
            Assert.True(proxy[50, 150] != 1);
            Assert.True(proxy[50, 50] != proxy[50, 150]);
            Assert.Equal(2, proxy[150, 50]);
        }

        #region Default U shape stroke
        private Point[] GetUShape(int topLeftOffsetX)
        {
            return new Point[] {
                    new Point(topLeftOffsetX,10), new Point(topLeftOffsetX,20),
                    new Point(topLeftOffsetX+10,20), new Point(topLeftOffsetX+10,10)
                };
        }

        private const int UShapeStrokeArea = 3 * 9 + 4;
        private const int UShapeClosedArea = 11 * 11;

        private void AssertUShapeInProxy(int topLeftOffsetX, int expectedBlobId, bool isShapeFilled = false)
        {
            if (!isShapeFilled)
            {
                for (int i = 0; i <= 10; i++)
                {
                    // Note: top row is not drawn in these tests
                    Assert.Equal(expectedBlobId, proxy[20, topLeftOffsetX + i]); // Bottom row
                    Assert.Equal(expectedBlobId, proxy[10 + i, topLeftOffsetX]); // Left column
                    Assert.Equal(expectedBlobId, proxy[10 + i, topLeftOffsetX + 10]); // Right column
                }
            }
            else
            {
                for (int i = 0; i <= 10; i++)
                    for (int j = 10; j <= 20; j++)
                        Assert.Equal(expectedBlobId,
                            proxy[j, topLeftOffsetX + i]);
            }
        }
        #endregion

        [Fact]
        public async Task ExtendingAsync()
        {
            // Stroke starting point blobId defines results blobId
            blobImage[10, 10] = 1;
            context.OperationRunEventArgs.Strokes = new Point[][] { GetUShape(10) };
            properties.Mode = ManualEditOperationProperties.ModeEnum.Extend;
            properties.IsStrokeClosed = false;
            await meo.Run(context, new Progress<double>(), token);

            AssertUShapeInProxy(10, 1);
            Assert.Equal(UShapeStrokeArea, proxy.GetMask(1).CountNonZero());
        }

        [Fact]
        public async Task ExtendingWithClosedStrokeAsync()
        {
            // Stroke starting point blobId defines results blobId
            blobImage[10, 10] = 1;
            context.OperationRunEventArgs.Strokes = new Point[][] { GetUShape(10) };
            properties.Mode = ManualEditOperationProperties.ModeEnum.Extend;
            properties.IsStrokeClosed = true;
            await meo.Run(context, new Progress<double>(), token);

            AssertUShapeInProxy(10, 1, true);
            Assert.Equal(UShapeClosedArea, proxy.GetMask(1).CountNonZero());
        }

        [Fact]
        public async Task AddWithoutStrokeAsync()
        {
            properties.Mode = ManualEditOperationProperties.ModeEnum.AddNew;
            properties.IsStrokeClosed = true;
            await meo.Run(context, new Progress<double>(), token);
            Assert.Equal(GetProxyPixelNumber(), proxy.GetMask(0).CountNonZero());
        }

        private int GetProxyPixelNumber()
        {
            Size size = proxy.Size;
            return size.Width * size.Height;
        }

        [Fact]
        public async Task AddingAsync()
        {
            context.OperationRunEventArgs.Strokes = new Point[][] { GetUShape(10) };
            properties.Mode = ManualEditOperationProperties.ModeEnum.AddNew;
            properties.IsStrokeClosed = true;

            Assert.Equal(GetProxyPixelNumber(), proxy.GetMask(0).CountNonZero());
            await meo.Run(context, new Progress<double>(), token);
            Assert.Equal(UShapeClosedArea, proxy.GetMask(1).CountNonZero());
            Assert.Equal(GetProxyPixelNumber() - UShapeClosedArea, proxy.GetMask(0).CountNonZero());
        }

        [Fact]
        public async Task SubtractingAsync()
        {
            context.OperationRunEventArgs.Strokes = new Point[][] { GetUShape(10) };
            properties.Mode = ManualEditOperationProperties.ModeEnum.Subtract;
            properties.IsStrokeClosed = true;

            blobImage.DrawBlobRect(5, 5, 10, 10, blobId: 1);
            blobImage.DrawBlobRect(15, 5, 10, 10, blobId: 2);
            Assert.Equal(GetProxyPixelNumber() - 2 * 100, proxy.GetMask(0).CountNonZero());
            Assert.Equal(100, proxy.GetMask(1).CountNonZero());
            Assert.Equal(100, proxy.GetMask(2).CountNonZero());
            await meo.Run(context, new Progress<double>(), token);
            Assert.Equal(75, proxy.GetMask(1).CountNonZero());
            Assert.Equal(70, proxy.GetMask(2).CountNonZero());
            Assert.Equal(GetProxyPixelNumber() - 145, proxy.GetMask(0).CountNonZero());
        }

        [Fact]
        public async Task SelectingWithEmptyOrNullTagnameDoesNothingAsync()
        {
            context.OperationRunEventArgs.Strokes =
                new Point[][] { GetUShape(10) };
            properties.Mode = ManualEditOperationProperties.ModeEnum.Select;

            blobImage[10, 10] = 1;

            properties.TagName = null;
            await meo.Run(context, new Progress<double>(), token);
            Assert.Empty(proxy.GetTagsForBlob(1));

            properties.TagName = "";
            await meo.Run(context, new Progress<double>(), token);
            Assert.Empty(proxy.GetTagsForBlob(1));
        }

        [Fact]
        public async Task SelectingAsync()
        {
            const string tagname = "Selected";
            context.OperationRunEventArgs.Strokes = new Point[][] { GetUShape(10) };
            properties.Mode = ManualEditOperationProperties.ModeEnum.Select;
            properties.TagName = tagname;

            blobImage[10, 10] = 1;
            blobImage[20, 20] = 2;
            blobImage[30, 30] = 3;  // Not covered by stroke

            Assert.Empty(proxy.GetBlobsByTagValue(tagname));
            await meo.Run(context, new Progress<double>(), token);
            var blobIdsWithTag = proxy.GetBlobsByTagValue(tagname).ToArray();
            Assert.Contains(1, blobIdsWithTag);
            Assert.Contains(2, blobIdsWithTag);
            Assert.DoesNotContain(3, blobIdsWithTag);
        }

        [Fact]
        public async Task DeletingAsync()
        {
            context.OperationRunEventArgs.Strokes = new Point[][] {
                new Point[] {  new Point(5,5), new Point(15, 5) } };
            properties.Mode = ManualEditOperationProperties.ModeEnum.Delete;

            blobImage.DrawBlobRect(5, 5, 10, 10, blobId: 1);
            blobImage.DrawBlobRect(15, 5, 10, 10, blobId: 2);
            blobImage.DrawBlobRect(25, 5, 10, 10, blobId: 3);

            await meo.Run(context, new Progress<double>(), token);
            Assert.DoesNotContain(1, proxy.CollectAllRealBlobIds());
            Assert.DoesNotContain(2, proxy.CollectAllRealBlobIds());
            Assert.Contains(3, proxy.CollectAllRealBlobIds());
        }
    }
}
