using Cv4s.Common.Exceptions;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Common.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;

namespace Cv4s.Operations.RoiOperation
{
    public class MedianBlurOperation : OperationBase
    {
        private readonly IUIHandlerService _uiHandler;
        private readonly IImageSaveService _imageSaveService;

        public MedianBlurOperation(IUIHandlerService uIHandler, IImageSaveService imageSaveService)
        {
            _uiHandler = uIHandler;
            _imageSaveService = imageSaveService;
            Properties = new MedianBlurOperationProperties();

            RunEventArgs.IsCallableFromCanvas = false;
            RunEventArgs.IsCallableFromButton = true;
        }

        public string Name => nameof(MedianBlurOperation);
        private string FolderName = MedianBlurOperationProperties.DefaultFolderName;
        public OperationProperties DefaultOperationProperties => new MedianBlurOperationProperties();
        private string ResolutionFileName = "resolutions.txt";

        private System.Drawing.Bitmap MedianBlurFunction(System.Drawing.Bitmap image, int iteration, int ksize)
        {
            var matImage = image.ToMat();
            for (int i = 0; i < iteration; i++)
            {
                Cv2.MedianBlur(matImage, matImage, ksize);
            }

            return matImage.ToBitmap();
        }

        public async Task DegradeImages(IRawImageSource imageSource, IBlobImageSource blobImageSource)
        {
            Console.WriteLine("Reading params from user...");

            _uiHandler.ShowMeasurementEditor(this,imageSource,blobImageSource);

            var properties = Properties as MedianBlurOperationProperties;

            if (properties!.FolderName != "")
                FolderName = properties.FolderName;

            Console.WriteLine("Degrading Images in progress...");

            var degradedImages = imageSource.ToDictionary();

            Parallel.For(0, degradedImages.Count, (index, state) =>
            {
                degradedImages[index] = MedianBlurFunction(degradedImages[index],
                                                             properties.IterationNumberPerImage,
                                                             (int)properties.KSize);
            });

            Console.WriteLine("Images degraded, saving ...");

            string savePath = _imageSaveService.CreateDirectoryPath(imageSource.RawImagePaths.First(), FolderName);

            _imageSaveService.SaveFilesToDirectory(savePath, degradedImages.Values);

            Console.WriteLine("Images saved ...");

            _imageSaveService.SaveResolutionsToDirectory(savePath,
                                    imageSource.XResolution,
                                    imageSource.YResolution,
                                    imageSource.ZResolution,
                                    ResolutionFileName);

            Console.WriteLine("Resolutions saved ...");
            Console.WriteLine("Finishing Task .. ");
        }
    }
}
