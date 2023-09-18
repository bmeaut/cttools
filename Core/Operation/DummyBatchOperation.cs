using Core.Interfaces.Operation;
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
    public class DummyBatchOperation : MultiLayerParallelOperationBase
    {
        public override string Name => "DummyBatchOperation";

        public override OperationProperties DefaultOperationProperties => new DummyBatchOperationProperties();

        public override async Task<OperationContext> RunOneLayer(OperationContext context, int layer, IProgress<double> progress, CancellationToken token)
        {
            DummyBatchOperationProperties props = (DummyBatchOperationProperties)context.OperationProperties;
            var x = props.RectX;
            var y = props.RectY;
            var shape = props.Shape;

            var bi = context.BlobImages[layer];
            var mask = bi.GetEmptyMask();

            switch (shape)
            {
                case DummyBatchOperationProperties.ShapeEnum.Rectangle:
                    var rect = new Rect(x, y, mask.Cols / 10, mask.Rows / 10);
                    mask.Rectangle(rect, new Scalar(1), -1);
                    break;
                case DummyBatchOperationProperties.ShapeEnum.Circle:
                    mask.Circle(x, y, 10, new Scalar(255, 255, 255, 255));
                    break;
            }

            bi.SetBlobMask(mask, 1);
            bi.SetTag(1, "dummy", 42);
            return context;
        }

    }

    public class DummyBatchOperationProperties : MultiLayerOperationProtertiesBase
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
