using Core.Image;
using Core.Interfaces.Image;
using OpenCvSharp;
using System;
using System.Linq;
using Xunit;

namespace Core.Test.Image
{
    public class BlobImageProxyTests
    {
        private readonly BlobImage blobImage;
        private readonly BlobImageProxy proxy;

        public BlobImageProxyTests()
        {
            blobImage = BlobImageTestHelper.GetTestBlobImageA();
            proxy = blobImage.GetProxy();
        }

        #region Operations inside the proxy
        [Fact]
        public void GetPixelAndBlobsByTagViaProxy()
        {
            Assert.Equal(1, proxy.GetMask(1).CountNonZero());
            Assert.Equal(1, proxy.GetBlobsByTagValue(
                BlobImageTestHelper.TagName, BlobImageTestHelper.TagValue).Single());
        }

        [Fact]
        public void ModifyBlobsViaProxy_OnlyModifiesProxy()
        {
            const string tagname = "newTagName";
            BlobImageTestHelper.AssertBlobCountForTag(blobImage, tagname, 0);
            proxy.SetTag(1, tagname);
            BlobImageTestHelper.AssertBlobCountForTag(blobImage, tagname, 0);
            BlobImageTestHelper.AssertBlobCountForTag(proxy, tagname, 1);
        }

        [Fact]
        public void AddAlreadyExistingTag_BlobIsReturnedOnce()
        {
            BlobImageTestHelper.AssertBlobCountForTag(blobImage, BlobImageTestHelper.TagName, 1);
            proxy.SetTag(1, BlobImageTestHelper.TagName);
            BlobImageTestHelper.AssertBlobCountForTag(proxy, BlobImageTestHelper.TagName, 1);
        }

        [Fact]
        public void RemoveTagAddedOnlyInProxy()
        {
            const string newTag = "newtag";
            BlobImageTestHelper.AssertBlobCountForTag(blobImage, newTag, 0);
            proxy.SetTag(1, newTag);
            BlobImageTestHelper.AssertBlobCountForTag(proxy, newTag, 1);
            proxy.RemoveTag(1, newTag);
            BlobImageTestHelper.AssertBlobCountForTag(proxy, newTag, 0);
        }

        [Fact]
        public void RemoveTagAppliesOnlyToProxy()
        {
            BlobImageTestHelper.AssertBlobCountForTag(blobImage, BlobImageTestHelper.TagName, 1);
            proxy.RemoveTag(1, BlobImageTestHelper.TagName);
            BlobImageTestHelper.AssertBlobCountForTag(blobImage, BlobImageTestHelper.TagName, 1);
            BlobImageTestHelper.AssertBlobCountForTag(proxy, BlobImageTestHelper.TagName, 0);
        }

        [Fact]
        public void RemoveTagInProxyDoesNotAffectOriginal()
        {
            proxy.RemoveTag(1, BlobImageTestHelper.TagName);
            BlobImageTestHelper.AssertBlobCountForTag(blobImage, BlobImageTestHelper.TagName, 1);
            Assert.Single(blobImage.GetTagsForBlob(1));
        }

        [Fact]
        public void RemoveTag()
        {
            Assert.Single(blobImage.GetBlobsByTagValue(BlobImageTestHelper.TagName));
            BlobImageTestHelper.AssertBlobCountForTag(blobImage, BlobImageTestHelper.TagName, 1);
            proxy.RemoveTag(1, BlobImageTestHelper.TagName);
            BlobImageTestHelper.AssertBlobCountForTag(proxy, BlobImageTestHelper.TagName, 0);
            Assert.Empty(proxy.GetTagsForBlob(1));
        }

        [Fact]
        public void ModifyTagAfterAdding()
        {
            const string newTag = "newtag";
            BlobImageTestHelper.AssertBlobCountForTag(blobImage, newTag, 0);
            proxy.SetTag(1, newTag, 5);
            proxy.SetTag(1, newTag, 7);
            BlobImageTestHelper.AssertBlobCountForTagValue(proxy, newTag, 5, 0);
            BlobImageTestHelper.AssertBlobCountForTagValue(proxy, newTag, 7, 1);
            BlobImageTestHelper.AssertBlobCountForTag(proxy, newTag, 1);
        }

        [Fact]
        public void TagModificationAppliesOnlyToGivenBlob()
        {
            const string newTag = "newtag";
            BlobImageTestHelper.AddSinglePixelBlobToUpperLeft(blobImage, 2);

            proxy.SetTag(1, newTag);
            proxy.SetTag(2, newTag);
            BlobImageTestHelper.AssertBlobCountForTag(proxy, newTag, 2);
            proxy.RemoveTag(2, newTag);
            BlobImageTestHelper.AssertBlobCountForTag(proxy, newTag, 1);
        }

        [Fact]
        public void ProxyGetTagsForBlob_ReturnsTagsAddedOnlyInProxy()
        {
            const string newTag = "newtag";
            proxy.SetTag(1, newTag);
            BlobImageTestHelper.AssertTag(proxy, 1, newTag);
        }

        [Fact]
        public void SetBlobMask_RemoveFromAllOtherLocations()
        {
            var proxy_NoRemoval = (new BlobImage(10, 10)).GetProxy();
            BlobImageTestHelper.Assert_SetBlobMask_RemovalFromAllOtherLocations(
                proxy_NoRemoval, false);
            var proxy_Removal = (new BlobImage(10, 10)).GetProxy();
            BlobImageTestHelper.Assert_SetBlobMask_RemovalFromAllOtherLocations(
                proxy_Removal, true);
        }
        #endregion

        #region Proxy responses reflect changes and preserve unchanged data
        [Fact]
        public void BlobImageLocationMatchesOriginalUntilChanged()
        {
            Assert.Equal(BlobImageTestHelper.BlobID, proxy[0, 0]);
            proxy[0, 0] = 2;
            Assert.Equal(2, proxy[0, 0]);
        }

        [Fact]
        public void GetMaskHandlesUnchangedAndModifiedValuesCorrectly()
        {
            const int xyToKeep11 = 0;   // blobId 1 -> 1 (unchanged)
            const int xyToSet01 = 1;    // 0 -> 1
            const int xyToClear10 = 2;  // 1 -> 0
            blobImage[xyToKeep11, xyToKeep11] = 1;
            blobImage[xyToSet01, xyToSet01] = 0;
            blobImage[xyToClear10, xyToClear10] = 1;

            proxy[xyToSet01, xyToSet01] = 1;
            proxy[xyToClear10, xyToClear10] = 0;

            var mask = proxy.GetMask(1);
            Assert.NotEqual(0, mask.At<int>(xyToKeep11, xyToKeep11));
            Assert.NotEqual(0, mask.At<int>(xyToSet01, xyToSet01));
            Assert.Equal(0, mask.At<int>(xyToClear10, xyToClear10));
        }

        [Fact]
        public void BlobClearViaSetMaskAppliesCorrecly()
        {
            // Setting blobId to 0 via proxies SetMask
            var mask = proxy.GetEmptyMask();
            mask.At<byte>(1, 1) = 1;
            blobImage[1, 1] = 1;
            proxy.SetBlobMask(mask, 0);
            Assert.Equal(0, proxy[1, 1]);
        }

        #endregion

        #region Apply functionality
        [Fact]
        public void ApplyEmptyChangeToEmptyBlobImage_DoesNotChangeAnything()
        {
            var blobImage = new BlobImage(new Size(10, 10));
            var proxy = blobImage.GetProxy();
            proxy.ApplyToOriginal();
            Assert.Empty(blobImage.CollectAllRealBlobIds());
            Assert.Equal(100, blobImage.GetMask(0).CountNonZero());
        }

        [Fact]
        public void AddSinglePixelBlob()
        {
            proxy[0, 0] = 2;
            Assert.Contains(2, proxy.CollectAllRealBlobIds());
            Assert.DoesNotContain(2, blobImage.CollectAllRealBlobIds());
            proxy.ApplyToOriginal();
            Assert.Contains(2, blobImage.CollectAllRealBlobIds());
        }

        [Fact]
        public void ApplyingAddsUpdatesDeletesTagsToExistingAndNewBlobs()
        {
            const string newTag = "newtag";
            const string existingTagToUpdate = "existingTagToUpdate";
            const string existingTagToRemove = "existingTagToRemove";
            blobImage.SetTag(BlobImageTestHelper.BlobID, existingTagToUpdate, 1);
            blobImage.SetTag(BlobImageTestHelper.BlobID, existingTagToRemove, 1);

            const int newBlobId = 2;
            BlobImageTestHelper.AddSinglePixelBlobToUpperLeft(
                proxy, newBlobId);

            proxy.SetTag(BlobImageTestHelper.BlobID, newTag, 2);
            proxy.SetTag(BlobImageTestHelper.BlobID, existingTagToUpdate, 2);
            proxy.RemoveTag(BlobImageTestHelper.BlobID, existingTagToRemove);

            proxy.SetTag(newBlobId, newTag);

            Assert.False(blobImage.HasBlobInTagDictionary(newBlobId));
            Assert.True(blobImage.HasBlobInTagDictionary(BlobImageTestHelper.BlobID));
            BlobImageTestHelper.AssertTag(blobImage, BlobImageTestHelper.BlobID, existingTagToUpdate, null);
            BlobImageTestHelper.AssertTag(blobImage, BlobImageTestHelper.BlobID, existingTagToRemove, null);

            proxy.ApplyToOriginal();

            Assert.True(blobImage.HasBlobInTagDictionary(BlobImageTestHelper.BlobID));
            Assert.True(blobImage.HasBlobInTagDictionary(newBlobId));

            BlobImageTestHelper.AssertTag(blobImage, newBlobId, newTag, null);
            BlobImageTestHelper.AssertTag(blobImage, BlobImageTestHelper.BlobID, existingTagToUpdate, 2);
            BlobImageTestHelper.AssertMissingTag(blobImage, BlobImageTestHelper.BlobID, existingTagToRemove);
        }

        [Fact]
        public void BlobIdChangesAppliedCorrectly()
        {
            const int xyToUntouched11 = 0; // blobId 1 -> 1 (unchanged)
            const int xyToUpdate23 = 1;   // 2 -> 3
            const int xyToClear30 = 2;    // 3 -> 0
            const int xyToSet04 = 3;      // 0 -> 4
            blobImage[xyToUntouched11, xyToUntouched11] = 1;
            blobImage[xyToUpdate23, xyToUpdate23] = 2;
            blobImage[xyToClear30, xyToClear30] = 3;
            blobImage[xyToSet04, xyToSet04] = 0;
            Assert.Equal(1, proxy[xyToUntouched11, xyToUntouched11]);
            Assert.Equal(2, proxy[xyToUpdate23, xyToUpdate23]);
            Assert.Equal(3, proxy[xyToClear30, xyToClear30]);
            Assert.Equal(0, proxy[xyToSet04, xyToSet04]);

            proxy[xyToUpdate23, xyToUpdate23] = 3;
            proxy[xyToClear30, xyToClear30] = 0;
            proxy[xyToSet04, xyToSet04] = 4;
            Assert.Equal(1, proxy[xyToUntouched11, xyToUntouched11]);
            Assert.Equal(3, proxy[xyToUpdate23, xyToUpdate23]);
            Assert.Equal(0, proxy[xyToClear30, xyToClear30]);
            Assert.Equal(4, proxy[xyToSet04, xyToSet04]);

            proxy.ApplyToOriginal();
            Assert.Equal(1, blobImage[xyToUntouched11, xyToUntouched11]);
            Assert.Equal(3, blobImage[xyToUpdate23, xyToUpdate23]);
            Assert.Equal(0, blobImage[xyToClear30, xyToClear30]);
            Assert.Equal(4, blobImage[xyToSet04, xyToSet04]);
        }
        #endregion

        [Fact]
        public void QueryTagsOfBlobNotPresentInOriginal()
        {
            proxy.DrawBlobRect(2, 2, 5, 5, blobId: 42);
            var tags = proxy.GetTagsForBlob(42);
            // Should not throw KeyNotFoundException for example...
            Assert.Empty(tags);
        }

        [Fact]
        public void QueryNonexistentTag()
        {
            int? value = proxy.GetTagValueOrNull(1, "UnknownTagName");
            Assert.False(value.HasValue);
        }
    }
}
