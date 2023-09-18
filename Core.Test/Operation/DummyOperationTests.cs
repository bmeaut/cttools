using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Core.Test.Operation
{
    public class DummyOperationTests
    {
        [Fact]
        public void DummyOperationWorks()
        {
            IOperation op = new DummyOperation();
            OperationContext context = new OperationContext();
            context.OperationProperties = op.DefaultOperationProperties;
            BlobImage blobImage = new BlobImage(10, 10);

            var proxy = blobImage.GetProxy();

            context.BlobImages = new Dictionary<int, IBlobImage>
            {
                { 0, proxy }
            };
            context.ActiveLayer = 0;
            context.OperationProperties = new DummyOperationProperties
            {
                RectX = 0,
                RectY = 0,
                Shape = DummyOperationProperties.ShapeEnum.Rectangle
            };

            Assert.Equal(0, proxy[0, 0]);
            op.Run(context);
            Assert.Equal(1, proxy[0, 0]);
            Assert.Equal(0, blobImage[0, 0]);
            proxy.ApplyToOriginal();
            Assert.Equal(1, blobImage[0, 0]);
            Assert.Equal(1, blobImage.GetMask(1).CountNonZero());
            Assert.Single(blobImage.GetBlobsByTagValue("dummy"));
        }
    }
}
