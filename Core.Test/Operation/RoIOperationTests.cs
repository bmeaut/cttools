using Core.Image;
using Core.Interfaces.Image;
using Core.Operation;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Core.Test.Operation
{
    public class RoIOperationTests
    {
        private readonly RoIOperation op = new RoIOperation();
        private readonly OperationContext context;
        private readonly CancellationToken token = new CancellationToken();
        private RoIOperationProperties props =>
            context.OperationProperties as RoIOperationProperties;


        public RoIOperationTests()
        {
            context = OperationTestsHelper.CreateOperationContext(op);

            var backgroundRect = new Rect(0, 0, context.ActiveBlobImage.Size.Width, context.ActiveBlobImage.Size.Height);

            // Layer 0 blobs
            var blobImage0 = context.BlobImages[0];
            blobImage0.DrawBlobRect(backgroundRect, blobId: 1);
            blobImage0.SetTag(blobId: 1, Tags.ComponentId.ToString(), 0);

            blobImage0.DrawBlobRect(Layer0Rects[0], blobId: 2);
            blobImage0.DrawBlobRect(Layer0Rects[1], blobId: 3);
            blobImage0.SetTag(blobIds: new List<int>() { 2, 3 }, Tags.ComponentId.ToString(), 1);
            blobImage0.SetTag(3, RoIOperation.RemoveFromRoiTagName);

            // Layer 1 blobs
            var blobImage1 = context.BlobImages[1];
            blobImage1.DrawBlobRect(backgroundRect, blobId: 1);
            blobImage1.SetTag(blobId: 1, Tags.ComponentId.ToString(), 0);

            blobImage1.DrawBlobRect(Layer1Rects[0], blobId: 2);
            blobImage1.DrawBlobRect(Layer1Rects[1], blobId: 3);
            blobImage1.SetTag(blobIds: new List<int>() { 2, 3 }, Tags.ComponentId.ToString(), 1);
        }

        private readonly Rect[] Layer0Rects = new Rect[]
        {
            new Rect(0, 0, 10, 10),
            new Rect(20, 0, 10, 10), // Premarked
        };

        private readonly Rect[] Layer1Rects = new Rect[]
        {
            new Rect(0, 20, 10, 20),
            new Rect(20, 0, 10, 10),
        };

        private Point[] GetStrokes(Rect rect)
        {
            return new Point[]
            {
                new Point(rect.X, rect.Y), new Point(rect.X + rect.Width - 1, rect.Y),
                new Point(rect.X + rect.Width - 1, rect.Y + rect.Height - 1), new Point(rect.X, rect.Y + rect.Height - 1)
            };
        }

        [Fact]
        public async void MarkSelectedBlobs()
        {
            props.Mode = RoIOperationProperties.ModeEnum.MarkBlobsToRemove;
            context.ActiveLayer = 1;
            var pointInBackgroundBlob = new Point(40, 40);
            var pointInFirstBlob = new Point(2, 22);
            context.OperationRunEventArgs.Strokes = new Point[][] { new Point[] { pointInBackgroundBlob, pointInFirstBlob } };

            await op.Run(context, new Progress<double>(), token);

            int secondBlobArea = GetArea(Layer1Rects[1]);
            int imageArea = context.ActiveBlobImage.Size.Width * context.ActiveBlobImage.Size.Height;
            int markedArea = GetMarkedArea(context.ActiveLayer);
            Assert.Equal(imageArea - secondBlobArea, markedArea);
        }

        [Fact]
        public async void MarkSelectedArea()
        {
            props.Mode = RoIOperationProperties.ModeEnum.MarkAreaToRemove;
            context.ActiveLayer = 0;
            var overlappingRect = new Rect(5, 5, 20, 5);
            var premarkedRect = Layer0Rects[1];
            context.OperationRunEventArgs.Strokes = new Point[][] { GetStrokes(overlappingRect) };
            
            await op.Run(context, new Progress<double>(), token);

            int preMarkedBlobArea = GetArea(premarkedRect);
            int selectedArea = GetArea(overlappingRect);
            int intersectionArea = GetArea(overlappingRect.Intersect(premarkedRect));
            int markedArea = GetMarkedArea(context.ActiveLayer);
            Assert.Equal(preMarkedBlobArea + selectedArea - intersectionArea, markedArea);
        }

        [Fact]
        public async void UnmarkSelectedArea()
        {
            props.Mode = RoIOperationProperties.ModeEnum.UnmarkArea;
            context.ActiveLayer = 0;
            var overlappingRect = new Rect(5, 5, 20, 5);
            var premarkedRect = Layer0Rects[1];
            context.OperationRunEventArgs.Strokes = new Point[][] { GetStrokes(overlappingRect) };

            await op.Run(context, new Progress<double>(), token);

            int preMarkedBlobArea = GetArea(premarkedRect);
            int intersectionArea = GetArea(overlappingRect.Intersect(premarkedRect));
            int markedArea = GetMarkedArea(context.ActiveLayer);
            Assert.Equal(preMarkedBlobArea - intersectionArea, markedArea);
        }

        [Fact]
        public async void MarkSelectedArea_DoesNotChangeComponents()
        {
            props.Mode = RoIOperationProperties.ModeEnum.MarkAreaToRemove;
            context.ActiveLayer = 0;
            var overlappingRect = new Rect(5, 5, 20, 5);
            context.OperationRunEventArgs.Strokes = new Point[][] { GetStrokes(overlappingRect) };

            await AssertOperationDoesNotChangeComponents();
        }

        [Fact]
        public async void UnmarkSelectedArea_DoesNotChangeComponents()
        {
            props.Mode = RoIOperationProperties.ModeEnum.UnmarkArea;
            context.ActiveLayer = 0;
            var overlappingRect = new Rect(5, 5, 20, 5);
            context.OperationRunEventArgs.Strokes = new Point[][] { GetStrokes(overlappingRect) };

            await AssertOperationDoesNotChangeComponents();
        }

        [Fact]
        public async void MarkLayer()
        {
            props.Mode = RoIOperationProperties.ModeEnum.MarkLayerToRemove;
            context.ActiveLayer = 0;

            await op.Run(context, new Progress<double>(), token);

            int imageArea = context.ActiveBlobImage.Size.Width * context.ActiveBlobImage.Size.Height;
            int markedArea = GetMarkedArea(context.ActiveLayer);
            Assert.Equal(imageArea, markedArea);
        }

        [Fact]
        public async void RemoveMarkedAreas()
        {
            props.Mode = RoIOperationProperties.ModeEnum.MarkAreaToRemoveOnAllLayers;
            var rectToMark = new Rect(50, 50, 10, 20);
            context.OperationRunEventArgs.Strokes = new Point[][] { GetStrokes(rectToMark) };
            await op.Run(context, new Progress<double>(), token);
            props.Mode = RoIOperationProperties.ModeEnum.RemoveMarkedAreasFromRoi;

            await op.Run(context, new Progress<double>(), token);

            int markedArea = GetMarkedArea(context.ActiveLayer);
            Assert.Equal(0, markedArea);

            int rectToMarkArea = GetArea(rectToMark);
            int premarkedLayer0Area = GetArea(Layer0Rects[1]);
            int imageArea = context.ActiveBlobImage.Size.Width * context.ActiveBlobImage.Size.Height;

            int layer0RoiArea = GetComponentArea(layerIndex: 0, componentId: 0) + GetComponentArea(layerIndex: 0, componentId: 1);
            Assert.Equal(imageArea - premarkedLayer0Area - rectToMarkArea, layer0RoiArea);

            int layer1RoiArea = GetComponentArea(layerIndex: 1, componentId: 0) + GetComponentArea(layerIndex: 1, componentId: 1);
            Assert.Equal(imageArea - rectToMarkArea, layer1RoiArea);
        }

        private int GetTagArea(int layerIndex, Tag tag)
        {
            var blobImage = context.BlobImages[layerIndex];
            var blobs = blobImage.GetBlobsByTagValue(tag.Name, tag.Value);
            Mat tagMask = blobImage.GetMaskUnion(blobs);
            return tagMask.CountNonZero();
        }

        private int GetComponentArea(int layerIndex, int componentId)
            => GetTagArea(layerIndex, new Tag(Tags.ComponentId.ToString(), componentId));

        private int GetMarkedArea(int layerindex)
            => GetTagArea(layerindex, new Tag(RoIOperation.RemoveFromRoiTagName, 0));

        private int GetArea(Rect rect)
        {
            return rect.Width * rect.Height;
        }

        private async Task AssertOperationDoesNotChangeComponents()
        {
            int component0AreaBefore = GetComponentArea(context.ActiveLayer, 0);
            int component1AreaBefore = GetComponentArea(context.ActiveLayer, 1);

            await op.Run(context, new Progress<double>(), token);

            int component0AreaAfter = GetComponentArea(context.ActiveLayer, 0);
            int component1AreaAfter = GetComponentArea(context.ActiveLayer, 1);

            Assert.Equal(component0AreaBefore, component0AreaAfter);
            Assert.Equal(component1AreaBefore, component1AreaAfter);
        }
    }
}
