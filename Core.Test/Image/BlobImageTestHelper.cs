using Core.Image;
using Core.Interfaces.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Core.Test.Image
{
    class BlobImageTestHelper
    {
        public const string TagName = "test";
        public const int TagValue = 1;
        public const int BlobID = 1;

        /// <summary>
        /// Returns 10x10 BlobImage with BlobID1 at 0;0
        ///     with Tag ("test";1)
        /// </summary>
        /// <returns></returns>
        public static BlobImage GetTestBlobImageA()
        {
            var bi = new BlobImage(new OpenCvSharp.Size(10, 10));
            var mask = bi.GetEmptyMask();
            mask.At<int>(0, 0) = 1;
            bi.SetBlobMask(mask, BlobID);
            bi.SetTag(BlobID, TagName, TagValue);
            return bi;
        }

        // This test is applied for all IBlobImage implementations
        public static void Assert_SetBlobMask_RemovalFromAllOtherLocations(IBlobImage blobImage, bool doRemoveFromOtherLocations)
        {
            blobImage[0, 0] = 1;    // Initial presence of blob

            var mask = blobImage.GetEmptyMask();
            mask.At<int>(1, 1) = 1;
            blobImage.SetBlobMask(mask, 1,
                removeFromOtherLocations: doRemoveFromOtherLocations);
            Assert.Equal(1, blobImage[1, 1]);   // added BlobID 1
            Assert.Equal(0, blobImage[0, 1]);   // unchanged

            // Upper left pixel depends on removal settings.
            Assert.Equal(doRemoveFromOtherLocations ? 0 : 1,
                blobImage[0, 0]);   // removed BlobID 1
        }

        #region Assert methods
        public static void AssertBlobCountForTag(IBlobImage blobImage, string tagName, int expectedBlobCount)
        {
            Assert.Equal(expectedBlobCount, blobImage.GetBlobsByTagValue(tagName, null).Count());
        }

        public static void AssertBlobCountForTagValue(IBlobImage blobImage, string tagName, int tagValue, int expectedBlobCount)
        {
            Assert.Equal(expectedBlobCount, blobImage.GetBlobsByTagValue(tagName, tagValue).Count());
        }

        public static void AssertTag(IBlobImage blobImage, int blobId, string tagName, int? tagValueOrNullForAny = null)
        {
            var tags = blobImage.GetTagsForBlob(blobId);
            Assert.Contains(tags, t => t.Name == tagName);
            if (tagValueOrNullForAny.HasValue)
                Assert.Contains(tags,
                    t => t.Name == tagName && t.Value == tagValueOrNullForAny.Value);
        }

        public static void AssertMissingTag(IBlobImage blobImage, int blobId, string tagName)
        {
            var tags = blobImage.GetTagsForBlob(blobId);
            Assert.DoesNotContain(tags, t => t.Name == tagName);
        }
        #endregion

        public static void AddSinglePixelBlobToUpperLeft(IBlobImage blobImage, int blobId)
        {
            var mask = blobImage.GetEmptyMask();
            mask.At<int>(0, 0) = 1;
            blobImage.SetBlobMask(mask, blobId);
        }

    }
}
