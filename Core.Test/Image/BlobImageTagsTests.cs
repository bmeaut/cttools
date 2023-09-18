using Core;
using Core.Image;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Core.Test.Image
{
    public class BlobImageTagsTests
    {
        private readonly BlobImage blobImage = new BlobImage(new Size(10, 10));
        private const int blobId = 3;
        private const string tagName = "selected";

        public BlobImageTagsTests()
        {
            // Create blobs...
            for (int i = 0; i < 5; i++)
            {
                var mask = blobImage.GetEmptyMask();
                mask.At<byte>(i, i) = 1;
                blobImage.SetBlobMask(mask, i);
            }
        }

        private bool HasTag(BlobImage blobImage, int blobId, string tagName)
        {
            return blobImage.GetTagsForBlob(blobId).Any(t => t.Name == tagName);
        }

        private bool HasTag(BlobImage blobImage, int blobId, string tagName, int value)
        {
            return blobImage.GetTagsForBlob(blobId)
                .Any(t => t.Name == tagName && t.Value == value);
        }

        [Fact]
        public void ModifyValueOfExistingTag()
        {
            const string unmodifiedTagName = "unmodified";
            Assert.False(HasTag(blobImage, blobId, tagName));
            blobImage.SetTag(blobId, tagName, 1);
            blobImage.SetTag(blobId, unmodifiedTagName, 2);
            Assert.True(HasTag(blobImage, blobId, tagName));
            Assert.True(HasTag(blobImage, blobId, tagName, 1));
            blobImage.SetTag(blobId, tagName, 3);
            Assert.False(HasTag(blobImage, blobId, tagName, 2));
            Assert.True(HasTag(blobImage, blobId, tagName, 3));
            Assert.True(HasTag(blobImage, blobId, unmodifiedTagName, 2));
        }

        [Fact]
        public void PresenceOfTagWithoutValue()
        {
            Assert.False(HasTag(blobImage, blobId, tagName));
            blobImage.SetTag(blobId, tagName);
            Assert.True(HasTag(blobImage, blobId, tagName));
        }

        [Fact]
        public void HandleTagValue()
        {
            Assert.False(HasTag(blobImage, blobId, tagName, 1));
            blobImage.SetTag(blobId, tagName, 1);
            Assert.True(HasTag(blobImage, blobId, tagName, 1));
            Assert.False(HasTag(blobImage, blobId, tagName, 2));
        }

        [Fact]
        public void QueryBlobsByTag()
        {
            Assert.Empty(blobImage.GetBlobsByTagValue(tagName));
            blobImage.SetTag(1, tagName, 1);
            blobImage.SetTag(2, tagName, 1);
            blobImage.SetTag(3, tagName, 2);
            blobImage.SetTag(1, "dummy");
            Assert.Equal(3, blobImage.GetBlobsByTagValue(tagName).Count());
            Assert.Equal(2, blobImage.GetBlobsByTagValue(tagName, 1).Count());
            Assert.Empty(blobImage.GetBlobsByTagValue("other", 0));
        }

        [Fact]
        public void RemoveTag()
        {
            blobImage.RemoveTag(blobId, tagName); // Nothing should happen
            Assert.False(HasTag(blobImage, blobId, tagName));
            blobImage.SetTag(blobId, tagName);
            Assert.True(HasTag(blobImage, blobId, tagName));
            blobImage.RemoveTag(blobId, tagName);
            Assert.False(HasTag(blobImage, blobId, tagName));
        }

        [Fact]
        public void QueryTagsOfBlob()
        {
            string[] addedTags = new string[] { "tag1", "tag2" };
            foreach (var tag in addedTags)
                blobImage.SetTag(blobId, tag);
            var result = blobImage.GetTagsForBlob(blobId);
            Assert.Equal(addedTags, result.Select(r => r.Name));
        }

        [Fact]
        public void QueryTagsOfNonExistingBlob()
        {
            var result = blobImage.GetTagsForBlob(blobId);
            Assert.Empty(result);
        }

        [Fact]
        public void AccessNonexistingBlobs()
        {
            Assert.Throws<KeyNotFoundException>(() => blobImage.SetTag(1000, "tagName"));
        }
    }
}
