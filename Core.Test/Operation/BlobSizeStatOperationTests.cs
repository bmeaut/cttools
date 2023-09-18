using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation;
using OpenCvSharp;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Core.Test.Operation
{
    public class BlobSizeStatOperationTests
    {
        private readonly BlobSizeStatOperation op;
        private readonly OperationContext context;
        private readonly CancellationToken token = new CancellationToken();

        public BlobSizeStatOperationTests()
        {
            op = new BlobSizeStatOperation();
            context = new OperationContext();
            context.BlobImages = new Dictionary<int, IBlobImage>();
            InitBlobImages();
            context.OperationProperties = op.DefaultOperationProperties;
            context.RawImageMetadata = new RawImageMetadata() { XResolution = 1.0 };
        }

        [Fact]
        public void CumulativeBlobCountForMaximalArea()
        {
            RunOnTestImage(BlobSizeStatOperationProperties.StatisticEnum.CumulariveBlobCountForMaximalArea);
            var scatterpoints = GetScatterPointsOfFirstInternalOutput(context);

            Assert.Equal(3, scatterpoints.Count());
            AssertScatterPoint(scatterpoints, 10 * 10, 1);
            AssertScatterPoint(scatterpoints, 40 * 40, 1 + 2);
            AssertScatterPoint(scatterpoints, 50 * 50, 1 + 2 + 1);
        }

        [Fact]
        public void AddsAreaTags()
        {
            RunOnTestImage(BlobSizeStatOperationProperties.StatisticEnum.CumulariveBlobCountForMaximalArea);
            for (int i = 0; i < Layer0BlobRects.Length; i++)
            {
                var rectArea = Layer0BlobRects[i].Width * Layer0BlobRects[i].Height;
                Assert.Equal(rectArea, context.ActiveBlobImage.GetTagValueOrNull(i + 1,
                    BlobSizeStatOperation.AreaInPixelTagName));
            }
        }

        private async void RunOnTestImage(BlobSizeStatOperationProperties.StatisticEnum statistic)
        {
            (context.OperationProperties as BlobSizeStatOperationProperties)
                .Statistic = statistic;
            await op.Run(context, new Progress<double>(), token);
        }

        private IEnumerable<ScatterPoint> GetScatterPointsOfFirstInternalOutput(OperationContext context, int seriesIndex = 0)
        {
            context.InternalOutputs.TryGetValue(BlobSizeStatOperation.OxyplotOutputName, out InternalOutput internalOutput);
            var series = (internalOutput as OxyplotInternalOutput)
                .PlotModel.Series.First() as ScatterSeries;
            return series.ItemsSource.Cast<ScatterPoint>();
        }

        private readonly Rect[] Layer0BlobRects = new Rect[]
            {
                // Areas: 100, 1600, 1600, 2500 pixels
                new Rect(10, 10, 10, 10),
                new Rect(50, 50, 40, 40),
                new Rect(50, 100, 40, 40),
                new Rect(100, 10, 50, 50)
            };

        private readonly Rect[] Layer1BlobRects = new Rect[]
            {
                // Areas: 200, 300, 400 pixels
                new Rect(10, 10, 20, 10),
                new Rect(50, 50, 30, 10),
                new Rect(50, 100, 40, 10)
            };

        private void InitBlobImages()
        {
            context.BlobImages.Add(0, new BlobImage(200, 200).GetProxy());
            context.BlobImages.Add(1, new BlobImage(200, 200).GetProxy());
            for (int i = 0; i < Layer0BlobRects.Length; i++)
                context.BlobImages[0].DrawBlobRect(Layer0BlobRects[i], blobId: i + 1);
            for (int i = 0; i < Layer1BlobRects.Length; i++)
                context.BlobImages[1].DrawBlobRect(Layer1BlobRects[i], blobId: i + 1);
            context.ActiveLayer = 0;
        }

        private void AssertScatterPoint(IEnumerable<ScatterPoint> scatterpoints, int x, int y)
        {
            Assert.Contains(scatterpoints, i => i.X == x && i.Y == y);
        }
    }
}
