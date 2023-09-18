using Core.Image;
using Core.Interfaces.Image;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Core.Test.Image
{
    public class BlobImageMementoTests
    {
        private readonly BlobImage blobImage;
        private readonly BlobImageProxy proxy;

        private const int xyToUntouched11 = 0; // blobId 1 -> 1 (unchanged)
        private const int xyToUpdate23 = 1;   // 2 -> 3
        private const int xyToSet04 = 3;      // 0 -> 4
        private const int xyToClear50 = 2;    // 5 -> 0

        private const int blobIdToUntouch = 1;
        private const int blobIdToUpdateOld = 2;
        private const int blobIdToUpdateNew = 3;
        private const int blobIdToAdd = 4;
        private const int blobIdToRemove = 5;

        private const int blobIdForTags = 1;
        private const int oldTagValue = 1;
        private const int newTagValue = 2;
        private const string newTag = "newtag";
        private const string existingTagToUpdate = "existingTagToUpdate";
        private const string existingTagToRemove = "existingTagToRemove";

        public BlobImageMementoTests()
        {
            blobImage = new BlobImage(10, 10);
            proxy = blobImage.GetProxy();
            PrepareBlobsAndTags();
        }

        private void PrepareBlobsAndTags()
        {
            // xy prefix: coordinate used both as x and y
            // Untouched11: this blob is not changed by proxy (remains 1)
            // Update23: blobId is updated from 2 to 3 by proxy
            // Set04: blobId set from 0 to 4 by proxy
            // Clear50: blobId cleared from 5 to 0 by proxy

            // Tags: only "blobIdForTags" is affected.
            // Old setup:
            //  existingTagToUpdate, oldTagValue
            //  existingTagToRemove (value remains default)
            // After applying the proxy:
            //  newTag
            //  existingTagToUpdate, newTagValueAfterUpdate
            //  (removed existingTagToRemove)

            // Prepare blobImage and proxy
            blobImage[xyToUntouched11, xyToUntouched11] = blobIdToUntouch;
            blobImage[xyToUpdate23, xyToUpdate23] = blobIdToUpdateOld;
            blobImage[xyToSet04, xyToSet04] = 0;
            blobImage[xyToClear50, xyToClear50] = blobIdToRemove;
            proxy[xyToUpdate23, xyToUpdate23] = blobIdToUpdateNew;
            proxy[xyToSet04, xyToSet04] = blobIdToAdd;
            proxy[xyToClear50, xyToClear50] = 0;

            // Prepare tags
            blobImage.SetTag(blobIdForTags, existingTagToUpdate, oldTagValue);
            blobImage.SetTag(blobIdForTags, existingTagToRemove);

            proxy.SetTag(blobIdForTags, newTag);
            proxy.SetTag(blobIdForTags,
                existingTagToUpdate, newTagValue);
            proxy.RemoveTag(blobIdForTags, existingTagToRemove);
        }

        [Fact]
        public void EmptyMementoCreatesEmptyProxy()
        {
            BlobImageMemento memento = new BlobImageMemento(blobImage);
            Assert.True(memento.IsIdentity);
            var undoProxy = memento.CreateUndoProxy();
            undoProxy.ApplyToOriginal();
            AssertOldSetup(blobImage);
        }

        [Fact]
        public void SingleTagChangeProxyInvertsCorrectly()
        {
            BlobImageTestHelper.AssertMissingTag(blobImage, blobIdForTags, newTag);
            var newProxy = blobImage.GetProxy();
            newProxy.SetTag(blobIdForTags, newTag);
            var memento = newProxy.ApplyToOriginal();
            Assert.False(memento.IsIdentity);
            BlobImageTestHelper.AssertTag(blobImage, blobIdForTags, newTag);
            var undoProxy = memento.CreateUndoProxy();
            undoProxy.ApplyToOriginal();
            BlobImageTestHelper.AssertMissingTag(blobImage, blobIdForTags, newTag);
        }

        [Fact]
        public void MementoCreatesCorrectUndoProxy()
        {
            AssertOldSetup(blobImage);
            BlobImageMemento memento = proxy.ApplyToOriginal();
            AssertNewSetup(blobImage);
            var undoProxy = memento.CreateUndoProxy();
            undoProxy.ApplyToOriginal();
            AssertOldSetup(blobImage);
        }

        private void AssertOldSetup(BlobImage blobImage)
        {
            Assert.Equal(blobIdToUntouch, blobImage[xyToUntouched11, xyToUntouched11]);
            Assert.Equal(blobIdToUpdateOld, blobImage[xyToUpdate23, xyToUpdate23]);
            Assert.Equal(blobIdToRemove, blobImage[xyToClear50, xyToClear50]);
            Assert.Equal(0, blobImage[xyToSet04, xyToSet04]);

            blobImage.RemoveUnusedTagDictionaryKeys();
            Assert.False(blobImage.HasBlobInTagDictionary(blobIdToAdd));

            BlobImageTestHelper.AssertMissingTag(
                blobImage, blobIdForTags, newTag);
            BlobImageTestHelper.AssertTag(
                blobImage, blobIdForTags, existingTagToUpdate, oldTagValue);
            BlobImageTestHelper.AssertTag(
                blobImage, blobIdForTags, existingTagToRemove, null);
        }

        private void AssertNewSetup(BlobImage blobImage)
        {
            Assert.Equal(blobIdToUntouch, blobImage[xyToUntouched11, xyToUntouched11]);
            Assert.Equal(blobIdToUpdateNew, blobImage[xyToUpdate23, xyToUpdate23]);
            Assert.Equal(blobIdToAdd, blobImage[xyToSet04, xyToSet04]);
            Assert.Equal(0, blobImage[xyToClear50, xyToClear50]);

            blobImage.RemoveUnusedTagDictionaryKeys();
            Assert.False(blobImage.HasBlobInTagDictionary(blobIdToRemove));

            BlobImageTestHelper.AssertTag(
                blobImage, blobIdForTags, newTag);
            BlobImageTestHelper.AssertTag(
                blobImage, blobIdForTags, existingTagToUpdate, newTagValue);
            BlobImageTestHelper.AssertMissingTag(
                blobImage, blobIdForTags, existingTagToRemove);
        }
    }
}
