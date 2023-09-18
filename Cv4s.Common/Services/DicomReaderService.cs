using Cv4s.Common.Enums;
using Cv4s.Common.Exceptions;
using Cv4s.Common.Interfaces.Images;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Render;
using System.Drawing;

namespace Cv4s.Common.Services
{
    public class DicomReaderService : IRawImageSource
    {
        private readonly Dictionary<int, DicomImage> _images = new Dictionary<int, DicomImage>();
        private DicomDataset _dataset;
        private BitDepth _bitdepth;
        private Color32[] _colorMap;

        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }

        // Resolutions are defined in mm. Z is the inter-slice dimension
        public double XResolution { get; set; }
        public double YResolution { get; set; }
        public double ZResolution { get; set; }

        public int DicomLevel { get; set; } = 200;
        public int DicomRange { get; set; } = 1000;

        public int NumOfLayers => _images.Count;

        public ScanFileFormat ScanFileFormat => ScanFileFormat.DICOM;

        public string[] RawImagePaths { get; set; }

        public DicomReaderService(string path)
        {
            var files = Directory.GetFiles(path, "*.dcm");
            ParseDicomFiles(files);
        }

        public DicomReaderService(string[] files)
        {
            ParseDicomFiles(files);
        }

        public Bitmap this[int sliceIndex]
        {
            get
            {
                var di = _images[sliceIndex];
                IImage iimage = GetImage(di);
                return iimage.AsClonedBitmap();
            }
        }

        private IImage GetImage(DicomImage di)
        {
            di.WindowCenter = DicomLevel;
            di.WindowWidth = DicomRange;
            return di.RenderImage();
        }

        public IDictionary<int, Bitmap> ToDictionary()
        {
            var dict = new Dictionary<int, Bitmap>();

            foreach (var key in _images.Keys)
            {
                dict.Add(key, this[key]);
            }

            return dict;
        }

        private void ParseDicomFiles(string[] files)
        {
            RawImagePaths = files;

            var images = new List<(DicomImage dicomImage, double zPosition)>();
            foreach (var file in files)
            {
                var dicomFile = DicomFile.Open(file);

                if (dicomFile == null)
                    throw new DicomFileException(dicomFile, $"Error loading DICOM files! Could not Load DICOM for : {file}");

                if (_dataset == null)
                {
                    _dataset = dicomFile.Dataset;
                }

                var dicomImage = new DicomImage(dicomFile.Dataset);
                double[] imagePositionPatient;
                dicomFile.Dataset.TryGetValues(DicomTag.ImagePositionPatient, out imagePositionPatient);
                images.Add((dicomImage, imagePositionPatient[2]));
            }

            var orderedImages = images.OrderByDescending(image => image.zPosition).ToList();

            var zDistances = new List<double>();
            for (int i = 0; i < orderedImages.Count - 1; i++)
            {
                zDistances.Add(orderedImages[i].zPosition - orderedImages[i + 1].zPosition);
            }

            for (int i = 0; i < orderedImages.Count; i++)
            {
                _images.Add(i, orderedImages[i].dicomImage);
            }
            if (zDistances.Count > 0)
            {
                var minZDistance = zDistances.Min();
                for (int i = 0; i < zDistances.Count; i++)
                {
                    if (zDistances[i] > 1.5 * minZDistance)
                    {
                        DicomImage dicomImage1 = orderedImages[i].dicomImage;
                        DicomImage dicomImage2 = orderedImages[i + 1].dicomImage;
                        int originalIndex1 = images.FindIndex(image => image.dicomImage == dicomImage1);
                        int originalIndex2 = images.FindIndex(image => image.dicomImage == dicomImage2);

                        throw new MissingLayerException(originalIndex2, originalIndex1);
                    }
                }
            }
            ImageManager.SetImplementation(WinFormsImageManager.Instance);

            var image = _images[0];
            ImageWidth = image.Width;
            ImageHeight = image.Height;

            var pixelSpacing = new double[2];
            _dataset.TryGetValues(DicomTag.PixelSpacing, out pixelSpacing);
            if (pixelSpacing == null)
            {
                pixelSpacing = new double[] { 0, 0 };
            }
            XResolution = pixelSpacing[0];
            YResolution = pixelSpacing[1];

            if (zDistances.Count > 0)
                ZResolution = zDistances.Average();
            else
                ZResolution = 0;
        }

        public int GetDicomPixelValue(int x, int y, int z)
        {
            var dPixelData = DicomPixelData.Create(_images[z].Dataset);
            IPixelData pixelData = PixelDataFactory.Create(dPixelData, 0);
            return (int)Math.Round(pixelData.GetPixel(x, y));
        }
    }
}
