using Core.Image;
using Core.Interfaces.Image;
using Core.Operation;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Core.Test.Operation
{
    public class ConnectedComponentsHelperTests
    {
        private readonly Mat image = new Mat(200, 200, MatType.CV_8UC1, new Scalar(0));

        #region GetConnectedComponentsMasksTests
        [Fact]
        public void GetConnectedComponentsMasks_MismatchingTypeImage_ThrowsError()
        {
            Assert.Throws<ArgumentException>(() => ConnectedComponentsHelper.GetConnectedComponentsMasks(null).ToArray());
            var mismatchingTypeImage = new Mat(200, 200, MatType.CV_8UC3);
            Assert.Throws<ArgumentException>(() => ConnectedComponentsHelper.GetConnectedComponentsMasks(mismatchingTypeImage).ToArray());
        }

        [Fact]
        public void GetConnectedComponentsMasks_SingleColorImageReturnsFullMask()
        {
            var img = new Mat(200, 200, MatType.CV_8UC1, new Scalar(255));
            Assert.Equal(200 * 200, ConnectedComponentsHelper.GetConnectedComponentsMasks(img).Single().CountNonZero());
        }

        [Fact]
        public void GetConnectedComponentsMasks_ImageWithSingleRectangle_ReturnsTwoMatchingMask()
        {
            Cv2.Rectangle(image, new Rect(10, 10, 10, 10), new Scalar(255), -1);
            var blobSizes = ConnectedComponentsHelper.GetConnectedComponentsMasks(image).Select(mask => mask.CountNonZero()).ToArray();
            Assert.Equal(2, blobSizes.Length);
            Assert.Contains(200 * 200 - 10 * 10, blobSizes);
            Assert.Contains(10 * 10, blobSizes);
        }

        [Fact]
        public void GetConnectedComponentsMasks_ImageWithTwoRectangles_ReturnsThreeMatchingMasks()
        {
            Cv2.Rectangle(image, new Rect(10, 10, 10, 10), new Scalar(255), -1);
            Cv2.Rectangle(image, new Rect(50, 50, 50, 50), new Scalar(255), -1);
            var masks = ConnectedComponentsHelper.GetConnectedComponentsMasks(image).ToArray();
            Assert.Equal(3, masks.Length);
            Assert.Equal(200 * 200 - (10 * 10) - (50 * 50), masks[0].CountNonZero());
            Assert.Equal(10 * 10, masks[1].CountNonZero());
            Assert.Equal(50 * 50, masks[2].CountNonZero());
        }
        #endregion

        [Fact]
        public void SegmentedBlobGetsSeparated()
        {
            var blobImage = InitBlobImage();
            ConnectedComponentsHelper.SeparateSegmentedPartsOfBlob(blobImage, 1);
            Assert.True(blobImage[20, 20] > 2);
            Assert.True(blobImage[20, 80] > 2);
            Assert.Equal(2, blobImage[20, 140]);
        }

        [Fact]
        public void MergedBlobIdsAreApplied()
        {
            var blobImage = InitBlobImage();
            ConnectedComponentsHelper.MergeBlobs(blobImage, 2, new int[] { 1 });
            Assert.Equal(2, blobImage[20, 20]);
            Assert.Equal(2, blobImage[20, 80]);
            Assert.Equal(2, blobImage[20, 140]);
        }

        private IBlobImage InitBlobImage()
        {
            BlobImage blobImage = new BlobImage(200, 200);
            blobImage.DrawBlobRect(20, 20, 40, 40, blobId: 1);
            blobImage.DrawBlobRect(80, 20, 40, 40, blobId: 1);
            blobImage.DrawBlobRect(140, 20, 40, 40, blobId: 2);
            return blobImage;
        }
    }
}
