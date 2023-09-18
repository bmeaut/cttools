using Core;
using Core.Image;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Core.Test.Image
{
    public class BlobImageBlobAccessTests
    {
        readonly BlobImage blobImage = new BlobImage(new Size(10, 10));

        [Fact]
        public void NoBlobsInAllZeroBlobImage()
        {
            Assert.False(blobImage.CollectAllRealBlobIds().Any());
        }

        private void DrawRectOnBlobImage(Rect rect, int blobId)
        {
            var mask = blobImage.GetEmptyMask();
            mask.Rectangle(rect, new Scalar(1));
            blobImage.SetBlobMask(mask, blobId);
        }

        [Fact]
        public void SingleBlob()
        {
            DrawRectOnBlobImage(new Rect(0, 0, 5, 5), 2);
            Assert.Single(blobImage.CollectAllRealBlobIds());
            Assert.Equal(2, blobImage.CollectAllRealBlobIds().Single());
        }

        [Fact]
        public void AddSingleBlobTwice_AddsOnlyOnce()
        {
            DrawRectOnBlobImage(new Rect(0, 0, 5, 5), 2);
            DrawRectOnBlobImage(new Rect(3, 3, 5, 5), 2);
            Assert.Single(blobImage.CollectAllRealBlobIds());
        }

        [Fact]
        public void AddTwoBlobs_AddsBoth()
        {
            DrawRectOnBlobImage(new Rect(0, 0, 5, 5), 2);
            DrawRectOnBlobImage(new Rect(3, 3, 5, 5), 3);
            Assert.Equal(2, blobImage.CollectAllRealBlobIds().Count());
        }

        [Fact]
        public void RemoveBlob()
        {
            DrawRectOnBlobImage(new Rect(0, 0, 5, 5), 2);
            DrawRectOnBlobImage(new Rect(0, 0, 5, 5), 0);
            Assert.False(blobImage.CollectAllRealBlobIds().Any());
        }
    }
}
