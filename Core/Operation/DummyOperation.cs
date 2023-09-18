using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation.OperationTools;
using OpenCvSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    /// <summary>
    /// Dummy operation to test basic interfaces.
    /// Sets upper half of the image to blobID 1 and adds the tag
    ///     "dummy",42 to the new blob.
    /// </summary>
    public class DummyOperation : IOperation
    {
        public string Name => "DummyOperation";

        public OperationProperties DefaultOperationProperties => new DummyOperationProperties();

        private Random random = new Random();

        // TODO: operation properties type validation
        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var operationProperties = (DummyOperationProperties)context.OperationProperties;
            var x = operationProperties.RectX;
            var y = operationProperties.RectY;
            var shape = operationProperties.Shape;

            var bi = context.BlobImages[context.ActiveLayer];
            var mask = bi.GetEmptyMask();

            Point[][] strokes = null;
            strokes = context.OperationRunEventArgs?.Strokes;

            if (strokes == null)
            {
                switch (shape)
                {
                    case DummyOperationProperties.ShapeEnum.Rectangle:
                        var rect = new Rect(x, y, mask.Cols / 10, mask.Rows / 10);
                        mask.Rectangle(rect, new Scalar(1), -1);
                        break;
                    case DummyOperationProperties.ShapeEnum.Circle:
                        mask.Circle(x, y, 10, new Scalar(255, 255, 255, 255));
                        break;
                }
            }
            if (strokes != null)
                mask = StrokeHelper.AddStrokesToMask(bi.GetEmptyMask(), strokes, true);

            var nextAvailableBlobId = bi.GetNextUnusedBlobId();
            bi.SetBlobMask(mask, nextAvailableBlobId);
            bi.SetTag(nextAvailableBlobId, "dummy", random.Next(0, 1000));
            return context;
        }
    }

    public class DummyOperationProperties : OperationProperties
    {
        public int RectX { get; set; }

        public int RectY { get; set; }

        public ShapeEnum Shape { get; set; }

        public enum ShapeEnum
        {
            Rectangle,
            Circle
        }
    }
}
