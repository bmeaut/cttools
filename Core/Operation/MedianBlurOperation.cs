using Core.Interfaces.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using System.IO;
using System.Drawing.Imaging;
using Core.Operation.OperationTools;
using OpenCvSharp.Extensions;

namespace Core.Operation
{
    public class MedianBlurOperation : IOperation
    {
        public string Name => nameof(MedianBlurOperation);
        private string FolderName = MedianBlurOperationProperties.DefaultFolderName;
        public OperationProperties DefaultOperationProperties => new MedianBlurOperationProperties();
        private string ResolutionFileName = "resolutions.txt";

        /// <summary>
        /// Opration for the OpenCV MedianBlur function.
        /// It smoothes the images with the given iteration number and kernelSize.
        /// After that is saves the images to the specified folder
        /// </summary>
        /// <param name="context"></param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var properties = context.OperationProperties as MedianBlurOperationProperties;

            if (properties.FolderName != "")
                FolderName = properties.FolderName;

            progress.Report(0.2);

            Parallel.For(0, context.RawImages.Count, (index, state) =>
            {
                if (token.IsCancellationRequested)
                    return;

                context.RawImages[index] = MedianBlurFunction(context.RawImages[index],
                                                             properties.IterationNumberPerImage,
                                                             (int)properties.KSize);
            });

            progress.Report(0.5);

            string savePath = ImageSaveOperationTool.CreateDirectoryPath(context.RawImageMetadata.RawImagePaths.First(), FolderName);

            progress.Report(0.6);

            ImageSaveOperationTool.SaveFilesToDirectory(savePath, context.RawImages.Values);

            progress.Report(0.8);

            ImageSaveOperationTool.SaveResolutionsToDirectory(savePath,
                                    context.RawImageMetadata.XResolution,
                                    context.RawImageMetadata.YResolution,
                                    context.RawImageMetadata.ZResolution,
                                    ResolutionFileName);
            progress.Report(1.0);

            return context;
        }

        private System.Drawing.Bitmap MedianBlurFunction(System.Drawing.Bitmap image, int iteration, int ksize)
        {
            var matImage = image.ToMat();
            for (int i = 0; i < iteration; i++)
            {
                Cv2.MedianBlur(matImage, matImage, ksize);
            }

            return matImage.ToBitmap();
        }

        private class MedianBlurOperationProperties : OperationProperties
        {
            public static string DefaultFolderName = "BluredImages";
            public enum KernelSize
            {
                Three = 3,
                Five = 5,
                Seven = 7,
                Eleven = 11,
                Thriteen = 13,
                Fifteen = 15,
            };

            public string FolderName { get; set; } = DefaultFolderName;
            public int IterationNumberPerImage { get; set; } = 3;
            public KernelSize KSize { get; set; } = KernelSize.Five;
        }
    }
}
