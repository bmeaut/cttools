using Core.Image;
using OpenCvSharp;
using System;
using System.Linq;
using Xunit;

namespace Core.Test.Image
{
    public class IBlobImageExtensionMethodsTests
    {
        [Fact]
        public void GetNextUnusedBlobId()
        {
            var blobImage = new BlobImage(new Size(10, 10));
            Assert.Equal(1, blobImage.GetNextUnusedBlobId());
            blobImage[0, 0] = 1;
            blobImage[1, 1] = 3;
            Assert.Equal(4, blobImage.GetNextUnusedBlobId());
        }

        [Fact]
        public void GetBlobIdsHitByMask_Works()
        {
            var blobImage = new BlobImage(200, 200);
            blobImage.DrawBlobRect(10, 10, 10, 10, blobId: 1);
            blobImage.DrawBlobRect(30, 10, 10, 10, blobId: 2);
            blobImage.DrawBlobRect(50, 10, 10, 10, blobId: 3);
            var mask = blobImage.GetEmptyMask();
            mask.Rectangle(new Rect(10, 10, 30, 10), new Scalar(255), -1);
            var ids = blobImage.GetBlobIdsHitByMask(mask).ToArray();
            Assert.Contains(1, ids);
            Assert.Contains(2, ids);
            Assert.DoesNotContain(3, ids);
            Assert.DoesNotContain(0, ids);
        }

        [Fact]
        public void GetHitBlobIds()
        {
            var blobImage = new BlobImage(200, 200);
            blobImage.DrawBlobRect(10, 10, 10, 10, blobId: 1);
            blobImage.DrawBlobRect(30, 10, 10, 10, blobId: 2);
            blobImage.DrawBlobRect(50, 10, 10, 10, blobId: 3);
            var stroke = new Point[][]
                { new Point[] { new Point(11, 11), new Point(12,11),
                  new Point(31,10) } };
            var ids = blobImage.GetHitBlobIds(stroke);
            Assert.Equal(2, ids.Length);
            Assert.Contains(1, ids);
            Assert.Contains(2, ids);
            Assert.DoesNotContain(3, ids);
            Assert.DoesNotContain(0, ids);
        }

        [Fact]
        public void GetMaskUnion()
        {
            var blobImage = new BlobImage(200, 200);
            blobImage.DrawBlobRect(10, 10, 10, 10, blobId: 1);
            blobImage.DrawBlobRect(30, 10, 10, 20, blobId: 2);
            blobImage.DrawBlobRect(50, 10, 10, 30, blobId: 3);
            var mask = blobImage.GetMaskUnion(new int[] { 1, 2 });
            Assert.Equal(10 * 10 + 10 * 20, mask.CountNonZero());
        }
    }
}
