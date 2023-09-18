using Core;
using Core.Image;
using Core.Interfaces.Image;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Windows.Media.Imaging;

namespace ViewModel
{
    /// <summary>
    /// Simple class to convert BlobImage objects to WPF WritableBitmap images.
    /// 
    /// Smarter implementations may reuse the temporal bgraImage object
    ///     if the required size did not change.
    /// 
    /// See also the OpenCvSharp.WpfExtensions BitmapSourceConverter:
    /// https://github.com/shimat/opencvsharp/blob/master/src/OpenCvSharp.WpfExtensions/BitmapSourceConverter.cs
    /// </summary>
    public class BlobImageConverter
    {
        /// <summary>
        /// Converts a BlobImage to a WriteableBitmap.
        /// </summary>
        /// <param name="blobImage"></param>
        /// <param name="converter">Typically the BlobAppearanceEngine</param>
        /// <param name="writeableBitmap">An already allocated image to reuse.</param>
        public static void Convert(BlobImage blobImage, IBlobId2ColorConverterService converter, WriteableBitmap writeableBitmap)
        {
            Mat bgraImage = new Mat(blobImage.Size, MatType.CV_8UC4);
            blobImage.GenerateBGRAImage(bgraImage, converter);
            WriteableBitmapConverter.ToWriteableBitmap(
                bgraImage, writeableBitmap);
        }
    }
}
