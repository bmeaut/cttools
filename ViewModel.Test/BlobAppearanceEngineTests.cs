using Core;
using Core.Image;
using OpenCvSharp;
using Core.Services;
using System;
using Xunit;

namespace ViewModel.Test
{
    public class BlobAppearanceEngineTests
    {
        private readonly BlobAppearanceEngineService engine;
        private readonly BlobImage blobImage = new BlobImage(new Size(10, 10));
        const int blobId = 1;
        readonly Tag tag = new Tag("TestTag", 1);
        readonly Vec4b color = new Vec4b(0, 0, 255, 255);

        public BlobAppearanceEngineTests()
        {
            engine = new BlobAppearanceEngineService()
            {
                AssignRandomDefaultColors = false,
                DefaultBlobColor = color
            };

            for (int i = 1; i < 5; i++)
            {
                var mask = blobImage.GetEmptyMask();
                mask.At<byte>(i, i) = 1;
                blobImage.SetBlobMask(mask, i);
            }
            engine.PrepareBlobs(blobImage);
        }

        [Fact]
        public void DefaultColorIsRed()
        {
            var black = new Vec4b(0, 0, 255, 255);
            for (int i = 1; i < 5; i++)
                Assert.Equal(black, engine[i]);
        }

        [Fact]
        public void SingleAppearanceCommandApplies()
        {
            blobImage.SetTag(blobId, tag.Name, tag.Value);
            engine.AddTagAppearanceCommand(new SimpleTagAppearanceCommand(tag.Name, color, 1));
            engine.PrepareBlobs(blobImage);
            Assert.Equal(color, engine[blobId]);
        }

        [Fact]
        public void AppearanceCommandWithHigherPriorityApplies()
        {
            // Two blobs to check independency of tag order
            const string tagA = "A";
            const string tagB = "B";
            blobImage.SetTag(1, tagA);
            blobImage.SetTag(1, tagB);
            blobImage.SetTag(2, tagB);
            blobImage.SetTag(2, tagA);

            var colorA = new Vec4b(0, 0, 255, 255);
            var colorB = new Vec4b(0, 255, 0, 0);
            engine.AddTagAppearanceCommand(new SimpleTagAppearanceCommand(tagA, colorA, 1));
            engine.AddTagAppearanceCommand(new SimpleTagAppearanceCommand(tagB, colorB, 2));

            engine.PrepareBlobs(blobImage);
            Assert.Equal(colorB, engine[1]);
            Assert.Equal(colorB, engine[2]);
        }
    }
}
