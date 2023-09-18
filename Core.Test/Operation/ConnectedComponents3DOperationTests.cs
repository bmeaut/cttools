using Core.Enums;
using Core.Image;
using Core.Interfaces.Image;
using Core.Operation;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Core.Test.Operation
{
    public class ConnectedComponents3DOperationTests
    {
        private readonly ConnectedComponents3DOperation op;
        private readonly OperationContext context;
        private readonly CancellationToken token = new CancellationToken();

        private readonly int layerCount = 3;
        private readonly MaterialTags material = MaterialTags.PORE;

        public ConnectedComponents3DOperationTests()
        {
            op = new ConnectedComponents3DOperation();
            context = new OperationContext();
            context.BlobImages = new Dictionary<int, IBlobImage>();
            InitBlobImages();
            context.OperationProperties = new ConnectedComponents3DOperationProperties() { Material = material };
        }

        [Fact]
        public async void ConnectedComponents()
        {
            await op.Run(context, new Progress<double>(), token);
            AssertMaterialTags(uShapeComponentRects, 1);
            AssertMaterialTags(boxComponentRects, 2);
        }

        private struct LayerRect
        {
            public int Layer;
            public Rect Rect;

            public LayerRect(int layer, Rect rect) => (Layer, Rect) = (layer, rect);
        }

        private readonly LayerRect[] uShapeComponentRects = new LayerRect[]
        {
            new LayerRect(0, new Rect(0, 0, 50, 10)),
            new LayerRect(1, new Rect(0, 0, 10, 10)),
            new LayerRect(1, new Rect(49, 0, 10, 10))
        };

        private readonly LayerRect[] boxComponentRects = new LayerRect[]
        {
            new LayerRect(0, new Rect(70, 70, 10, 10)),
            new LayerRect(1, new Rect(70, 70, 10, 10)),
            new LayerRect(2, new Rect(70, 70, 10, 10))
        };

        private void InitBlobImages()
        {
            context.BlobImages.Add(0, new BlobImage(100, 100));
            context.BlobImages.Add(1, new BlobImage(100, 100));
            context.BlobImages.Add(2, new BlobImage(100, 100));
            DrawComponent(uShapeComponentRects, 1);
            DrawComponent(boxComponentRects, 2);
        }

        private void DrawComponent(LayerRect[] layerRects, int id)
        {

            var blobimages = context.BlobImages;
            foreach (var layerRect in layerRects)
            {
                var layer = layerRect.Layer;
                var rect = layerRect.Rect;
                var blobImage = blobimages[layer];
                var blobId = blobImage.GetNextUnusedBlobId();
                blobImage.DrawBlobRect(rect, blobId);
                blobImage.SetTag(blobId, material.ToString(), id);
            }
        }

        private void AssertMaterialTags(LayerRect[] layerRects, int id)
        {
            var blobimages = context.BlobImages;
            foreach (var layerRect in layerRects)
            {
                var layer = layerRect.Layer;
                var rect = layerRect.Rect;
                var blobImage = blobimages[layer];
                var blobId = blobImage[rect.Y, rect.X];
                var tagValue = blobImage.GetTagValueOrNull(blobId, material.ToString());
                Assert.True(tagValue == id);
            }
        }
    }
}
