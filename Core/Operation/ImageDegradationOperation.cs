using Core.Interfaces.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;
using System.Drawing.Imaging;
using Newtonsoft.Json;
using Core.Operation.OperationTools;

namespace Core.Operation
{
    public class ImageDegradationOperation : IOperation
    {
        public string Name => nameof(ImageDegradationOperation);
        public OperationProperties DefaultOperationProperties => new ImageDegradationProperties();
        private InterpolationFlags InterpolationType;
        private int ResizeRatio;
        private string DirectoryName = "DegImages";
        private string ResolutionFileName = "resolutions.txt";

        /// <summary>
        /// Operation which takes the RawImages from the context and degrades it, with the given ratio and interpolation tchnique
        /// and saves it to the specified folder
        /// </summary>
        /// <param name="context"></param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var properties = context.OperationProperties as ImageDegradationProperties;
            ResizeRatio = properties.ResizeRatio;
            InterpolationType = (InterpolationFlags)properties.InterpolationType;
            DirectoryName = properties.DirectoryName == "" ? DirectoryName : properties.DirectoryName;

            progress.Report(0);

            if (properties.RunBatch)
            {
                //allImage
                Parallel.For(0, context.RawImages.Count, (index,state) =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    context.RawImages[index] = ImageQualityDegradation(context.RawImages[index]);
                });
            }
            else
            {
                //ActualImage
                var bitmap = context.RawImages[context.ActiveLayer];
                context.RawImages[context.ActiveLayer] = ImageQualityDegradation(bitmap);
            }

            progress.Report(0.5);

            if (token.IsCancellationRequested)
                return context;

            var directory = ImageSaveOperationTool.CreateDirectoryPath(context.RawImageMetadata.RawImagePaths.First(),DirectoryName);

            progress.Report(0.8);

            var ScanFilePaths = ImageSaveOperationTool.SaveFilesToDirectory(directory, context.RawImages.Values);

            ImageSaveOperationTool.SaveResolutionsToDirectory(directory, context.RawImageMetadata.XResolution,
                                                  context.RawImageMetadata.YResolution,
                                                  context.RawImageMetadata.ZResolution,ResolutionFileName);

            progress.Report(1.0);

            return context;
        }

        private System.Drawing.Bitmap ImageQualityDegradation(System.Drawing.Bitmap bitmap)
        {
            var imageMat = bitmap.ToMat();
            Size fullSize = imageMat.Size();
            Size halfSize = fullSize;
            halfSize.Height /= ResizeRatio;
            halfSize.Width /= ResizeRatio;
            imageMat = imageMat.Resize(halfSize);
            var interpolatedMat = imageMat.Resize(fullSize, 0, 0, InterpolationType);
            imageMat.Release();
            return BitmapConverter.ToBitmap(interpolatedMat);
        }
    }

    public class ImageDegradationProperties : OperationProperties
    {
        /// <summary>
        /// Choseable interpolation techniques
        /// Only a few, because there are some techinques which can throw exceptions
        /// </summary>
        public enum InterPolationType
        {
            Cubic = InterpolationFlags.Cubic,
            Linear = InterpolationFlags.Linear,
            LinearExact = InterpolationFlags.LinearExact,
            Area = InterpolationFlags.Area,
            Lanczos = InterpolationFlags.Lanczos4,
            Nearest = InterpolationFlags.Nearest
        };

        public int ResizeRatio { get; set; } = 2;
        public readonly bool RunBatch = true;
        public InterPolationType InterpolationType { get; set; } = InterPolationType.Cubic;
        public string DirectoryName { get; set; } = "DegImages_1_To_2";
    }
}
