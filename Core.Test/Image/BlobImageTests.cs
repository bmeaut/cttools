using Core;
using Core.Image;
using Core.Interfaces.Image;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Core.Test.Image
{
    public class BlobImageTests
    {

        private class DirectlyEditableBlobImage : BlobImage
        {
            public DirectlyEditableBlobImage(Size size) : base(size)
            {
            }

            public new int this[int y, int x]
            {
                get => base[y, x];
                set => base[y, x] = value;
            }
        }

        readonly DirectlyEditableBlobImage blobImage =
            new DirectlyEditableBlobImage(new Size(10, 10));

        [Fact]
        public void Instantiation()
        {
            Assert.Equal(0, blobImage[5, 5]);
            blobImage[5, 5] = 1;
            Assert.Equal(1, blobImage[5, 5]);
            Assert.Equal(0, blobImage[0, 0]); // Check boundary indexing
        }

        [Fact]
        public void InvalidIndexing_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => blobImage[-1, 5]);
            Assert.Throws<ArgumentOutOfRangeException>(() => blobImage[10, 5]);
            Assert.Throws<ArgumentOutOfRangeException>(() => blobImage[5, -1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => blobImage[5, 10]);
        }

        [Fact]
        public void EnsureInternalImageIsNotPublic()
        {
            Assert.False(
                typeof(BlobImage)
                .GetField("Image", BindingFlags.NonPublic | BindingFlags.Instance)
                .IsPublic);
        }

        [Fact]
        public void GetEmptyMaskOfCorrectSize()
        {
            Mat mask = blobImage.GetEmptyMask();
            Assert.Equal(MatType.CV_8UC1, mask.Type());
            Assert.Equal(0, mask.CountNonZero());
        }

        [Fact]
        public void GetMaskForBlob_ReturnsCorrectMask()
        {
            Mat mask = blobImage.GetMask(1);
            int nonzero = mask.CountNonZero();
            Assert.Equal(0, nonzero);
            Assert.Equal(10, mask.Rows);
            Assert.Equal(10, mask.Cols);

            blobImage[2, 2] = 1;
            mask = blobImage.GetMask(1);
            nonzero = mask.CountNonZero();
            Assert.Equal(1, nonzero);
            Assert.Equal(1, blobImage[2, 2]);
        }

        [Fact]
        public void SetBlobIdUsingMask()
        {
            var mask = new Mat(blobImage.Size, MatType.CV_8UC1, 0);

            // Mask can have any nonzero value to indicate true value.
            const int blobId = 5;
            mask.At<byte>(2, 2) = 1;
            mask.At<byte>(2, 3) = 255;
            Assert.Equal(0, blobImage[2, 2]);
            blobImage.SetBlobMask(mask, blobId);

            var maskForLabel = blobImage.GetMask(blobId);
            Assert.Equal(2, maskForLabel.CountNonZero());
        }

        [Fact]
        public void SetBlobMask_RemoveFromAllOtherLocations()
        {
            var blobImage_NoRemoval = new BlobImage(10, 10);
            BlobImageTestHelper.Assert_SetBlobMask_RemovalFromAllOtherLocations(
                blobImage_NoRemoval, false);
            var blobImage_Removal = new BlobImage(10, 10);
            BlobImageTestHelper.Assert_SetBlobMask_RemovalFromAllOtherLocations(
                blobImage_Removal, true);
        }

        private void PrepBlobImageWithRectOf2()
        {
            // Direct access to the label image is not allowed.
            // Ask for an empty mask, draw on that and use BlobImage.Set to apply.
            var mask = blobImage.GetEmptyMask();
            mask.Rectangle(new Rect(0, 0, 5, 5), new Scalar(1));
            blobImage.SetBlobMask(mask, 2);
        }

        [Fact]
        public void GenerateColorImageFromBlobImage()
        {
            PrepBlobImageWithRectOf2();
            var red = new Vec4b(0, 0, 255, 255);
            var green = new Vec4b(0, 255, 0, 255);
            var converter = new DictionaryBlobId2ColorConverter();
            converter[0] = red;
            converter[2] = green;
            var colorImage = blobImage.GenerateBGRAImage(converter);

            Assert.Equal(green, colorImage.At<Vec4b>(0, 0));
            Assert.Equal(red, colorImage.At<Vec4b>(8, 8));
        }
    }

    /// <summary>
    /// Very simple, dictionary based implementation of IBlobId2ColorConverter.
    /// </summary>
    public class DictionaryBlobId2ColorConverter : IBlobId2ColorConverterService
    {
        private readonly Dictionary<int, Vec4b> dict = new Dictionary<int, Vec4b>();

        public virtual Vec4b this[int blobId]
        {
            get
            {
                return dict[blobId];
            }
            set
            {
                dict[blobId] = value;
            }
        }

        public bool AssignRandomDefaultColors { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Vec4b DefaultBlobColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// Priority wont be used, which is not nice but this class is useless anyway because of BlobAppearanceEngine
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="color"></param>
        /// <param name="priority"></param>
        public void AddTagAppearanceCommand(string tagName, Vec4b color, int priority = 0)
        {
            this[0] = color;
        }

        public void AddTagAppearanceCommand(ITagAppearanceCommand tagAppearanceCommand)
        {
            throw new NotImplementedException();
        }

        public void PrepareBlobs(IBlobImage blobImage)
        {
        }

        public void SelectAppearance(int id)
        {
            throw new NotImplementedException();
        }
    }

}
