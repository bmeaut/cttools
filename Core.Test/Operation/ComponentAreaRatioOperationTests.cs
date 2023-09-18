using Core.Enums;
using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation;
using Core.Operation.OperationTools;
using OpenCvSharp;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Core.Operation.ComponentAreaRatioOperation;

namespace Core.Test.Operation
{
    public class ComponentAreaRatioOperationTests
    {
        private readonly ComponentAreaRatioOperation op;
        private readonly OperationContext context;
        private readonly CancellationToken token = new CancellationToken();

        private readonly int layerCount = 2;
        private readonly int roiSize = 80;
        private readonly MaterialTags material = MaterialTags.PORE;

        public ComponentAreaRatioOperationTests()
        {
            op = new ComponentAreaRatioOperation();
            context = new OperationContext();
            context.BlobImages = new Dictionary<int, IBlobImage>();
            InitBlobImages();
            context.OperationProperties = new ComponentAreaRatioOperationProperties() { Material = material };
        }

        [Fact]
        public void AreaRatioValuesForXAxis()
        {
            var direction = ImagePlane.Direction.X;
            RunOnTestImage(direction);
            var scatterpoints = GetScatterPoints(context, direction);

            Assert.Equal(100, scatterpoints.Count());

            int areaSum = layerCount * roiSize;
            AssertScatterPoint(scatterpoints, 1, 0);
            AssertScatterPoint(scatterpoints, 25, CalculateRatioPercentage(10 + 20, areaSum));
            AssertScatterPoint(scatterpoints, 40, 0);
            AssertScatterPoint(scatterpoints, 55, CalculateRatioPercentage(10 + 20, areaSum));
            AssertScatterPoint(scatterpoints, 90, 0);
        }

        [Fact]
        public void AreaRatioValuesForYAxis()
        {
            var direction = ImagePlane.Direction.Y;
            RunOnTestImage(direction);
            var scatterpoints = GetScatterPoints(context, direction);

            Assert.Equal(100, scatterpoints.Count());

            int areaSum = layerCount * roiSize;
            AssertScatterPoint(scatterpoints, 1, 0);
            AssertScatterPoint(scatterpoints, 25, CalculateRatioPercentage(10 + 25, areaSum));
            AssertScatterPoint(scatterpoints, 40, 0);
            AssertScatterPoint(scatterpoints, 55, CalculateRatioPercentage(22 + 10, areaSum));
            AssertScatterPoint(scatterpoints, 90, 0);
        }

        [Fact]
        public void AreaRatioValuesForZAxis()
        {
            var direction = ImagePlane.Direction.Z;
            RunOnTestImage(direction);
            var scatterpoints = GetScatterPoints(context, direction);

            Assert.Equal(2, scatterpoints.Count());

            int areaSum = roiSize * roiSize;
            AssertScatterPoint(scatterpoints, 1, CalculateRatioPercentage(10 * 10 + 22 * 20, areaSum));
            AssertScatterPoint(scatterpoints, 2, CalculateRatioPercentage(25 * 10 + 10 * 20, areaSum));
        }

        private int CalculateRatioPercentage(int area, int areaSum)
        {
            return (int)Math.Round((double)area / areaSum * 100.0);
        }

        private void RunOnTestImage(ImagePlane.Direction direction)
        {
            (context.OperationProperties as ComponentAreaRatioOperationProperties)
                .Direction = direction;
            op.Run(context, new Progress<double>(), token);
        }

        private IEnumerable<ScatterPoint> GetScatterPoints(OperationContext context, ImagePlane.Direction direction)
        {
            context.InternalOutputs.TryGetValue(ComponentAreaRatioOperation.GetAreaRatioHistogramName(direction, material.ToString()), out InternalOutput internalOutput);
            var series = (internalOutput as OxyplotInternalOutput)
                .PlotModel.Series.First() as ScatterSeries;
            return series.ItemsSource.Cast<ScatterPoint>();
        }

        private readonly Rect[] Layer0BlobRects = new Rect[]
            {
                new Rect(20, 20, 10, 10),
                new Rect(50, 50, 22, 20),
            };

        private readonly Rect[] Layer1BlobRects = new Rect[]
            {
                new Rect(50, 20, 25, 10),
                new Rect(20, 50, 10, 20),
            };

        private void InitBlobImages()
        {
            context.BlobImages.Add(0, new BlobImage(100, 100));
            context.BlobImages.Add(1, new BlobImage(100, 100));
            var roi = new Rect(10, 10, roiSize, roiSize);
            for (int i = 0; i < context.BlobImages.Count; i++)
            {
                int blobId = 1;
                context.BlobImages[i].DrawBlobRect(roi, blobId);
                context.BlobImages[i].SetTag(blobId, "ROITest");
            }
            for (int i = 0; i < Layer0BlobRects.Length; i++)
            {
                int blobId = i + 2;
                context.BlobImages[0].DrawBlobRect(Layer0BlobRects[i], blobId);
                context.BlobImages[0].SetTag(blobId, material.ToString());
            }
            for (int i = 0; i < Layer1BlobRects.Length; i++)
            {
                int blobId = i + 2;
                context.BlobImages[1].DrawBlobRect(Layer1BlobRects[i], blobId);
                context.BlobImages[1].SetTag(blobId, material.ToString());
            }
            context.ActiveLayer = 0;
        }

        private void AssertScatterPoint(IEnumerable<ScatterPoint> scatterpoints, int x, int y)
        {
            Assert.Contains(scatterpoints, i => i.X == x && i.Y == y);
        }
    }
}
