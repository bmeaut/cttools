using Core.Exceptions;
using Core.Interfaces.Image;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.LUT;
using Dicom.Imaging.Render;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Core.Image
{
    public class DicomReader : IRawImageSource
    {
        private readonly Dictionary<int, DicomImage> _images = new Dictionary<int, DicomImage>();
        DicomDataset _dataset;

        private BitDepth _bitdepth;
        private Color32[] _colorMap;

        public int NumberOfLayers { get; private set; }
        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }

        // Resolutions are defined in mm. Z is the inter-slice dimension
        public double XResolution { get; set; }
        public double YResolution { get; set; }
        public double ZResolution { get; set; }

        public int DicomLevel { get; set; } = 200;
        public int DicomRange { get; set; } = 1000;


        public DicomReader(string path)
        {
            var files = Directory.GetFiles(path, "*.dcm");
            ParseDicomFiles(files);
        }

        public DicomReader(string[] files)
        {
            ParseDicomFiles(files);
        }

        public Bitmap this[int sliceIndex]
        {
            get
            {
                var di = _images[sliceIndex];
                IImage iimage = GetImage(di);
                //IImage iimage = GetImageWithCustomLUT(di); 
                return iimage.AsClonedBitmap();
            }
        }

        private IImage GetImage(DicomImage di)
        {
            di.WindowCenter = DicomLevel;
            di.WindowWidth = DicomRange;
            return di.RenderImage();
        }

        private IImage GetImageWithCustomLUT(DicomImage di)
        {
            //var options = GrayscaleRenderOptions.CreateLinearOption(_bitdepth, DicomLevel, DicomRange);
            //var options = GrayscaleRenderOptions.CreateLinearOption(_bitdepth, 100, 3000);
            //var options = GrayscaleRenderOptions.FromHistogram(_dataset);

            //options.ColorMap = _colorMap;
            //OutputLUT lut = new OutputLUT(options);
            //var lut = new VOISigmoidLUT(options);
            //var lut = new VOILinearLUT(options);
            var lut = new CustomLut(DicomLevel, DicomRange);

            var dPixelData = DicomPixelData.Create(_dataset);
            IPixelData pixelData = PixelDataFactory.Create(dPixelData, 0);
            ImageGraphic ig = new ImageGraphic(pixelData);
            IImage iimage = ig.RenderImage(lut);
            return iimage;
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
            var images = new List<(DicomImage dicomImage, double zPosition)>();
            var filesThatCouldNotLoad = new List<string>();
            foreach (var file in files)
            {
                var filePath = Path.GetFullPath(file);
                var dicomFile = DicomFile.Open(@filePath);

                if (dicomFile == null)
                {
                    filesThatCouldNotLoad.Add(file);
                    continue;
                }

                if (_dataset == null)
                {
                    _dataset = dicomFile.Dataset;
                }

                var dicomImage = new DicomImage(dicomFile.Dataset);
                double[] imagePositionPatient;
                dicomFile.Dataset.TryGetValues(DicomTag.ImagePositionPatient, out imagePositionPatient);
                images.Add((dicomImage, imagePositionPatient[2]));
            }

            if (filesThatCouldNotLoad.Count > 0)
            {
                throw new CouldNotLoadDicomFilesException(filesThatCouldNotLoad);
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
            //ImageManager.SetImplementation(WPFImageManager.Instance); // TODO Check if this is any better

            NumberOfLayers = images.Count;

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

        /// <summary>
        /// Try to guess good values after loading. 
        /// Note:
        /// MaterialSample wraps Level and Range properties, so setting them is a must,
        /// but that also changes the value present in db.
        /// Null or default value checking could be the solution but it seems inappropriate now.
        /// </summary>
        public void SetContrastAndLevel()
        {
            int center_index = (int)(_images.Count / 2);
            if (center_index < 0 || center_index >= _images.Count) return;
            var dicomImage = _images[center_index];
            var dPixelData = DicomPixelData.Create(_dataset);
            IPixelData pixelData = PixelDataFactory.Create(dPixelData, 0);
            _bitdepth = dPixelData.BitDepth;

            var range = pixelData.GetMinMax();
            var min = range.Minimum;
            var max = range.Maximum;
            DicomLevel = (int)((min + max) / 2);
            DicomRange = (int)((max - min) / 2);

            _colorMap = new Color32[256];
            for (int i = 0; i < _colorMap.Length; i++)
            {
                var alpha = (byte)(i);
                _colorMap[i] = new Color32(0, alpha, alpha, alpha);
            }
        }

        public int GetDicomPixelValue(int x, int y, int z)
        {
            var dPixelData = DicomPixelData.Create(_images[z].Dataset);
            IPixelData pixelData = PixelDataFactory.Create(dPixelData, 0);
            return (int)Math.Round(pixelData.GetPixel(x, y));
        }
    }
    internal class CustomLut : ILUT
    {
        public int this[int value] => CalculateValueFromDicomValue(value);

        private double _range = 1000;
        private double _level = 0;
        private Color32[] colorMap;

        public CustomLut(int level, int range)
        {
            _level = level;
            _range = range;
            colorMap = new Color32[256];
            for (int i = 0; i < colorMap.Length; i++)
            {
                var alpha = (byte)(i);
                colorMap[i] = new Color32(0, alpha, alpha, alpha);
            }
        }

        private int CalculateValueFromDicomValue(int value)
        {
            int res = CalculateRawValue(value);
            if (res < 0) return colorMap[0].Value;
            if (res > 255) return colorMap[255].Value;
            return colorMap[res].Value;
        }

        private int CalculateRawValue(int value)
        {
            var s = Sigmoid(value);
            return (int)(s * 255);
        }

        public float Sigmoid(double value)
        {
            var c1 = -1.0 / (_range / 2);
            var c2 = _level;
            float k = expf(c1 * (value - c2));
            return 1 / (1.0f + k);
        }

        private static float expf(double value)
        {
            return (float)Math.Exp(value);
        }

        public bool IsValid => true;

        public int MinimumOutputValue => int.MinValue;

        public int MaximumOutputValue => int.MaxValue;

        public void Recalculate() { }
    }
}
