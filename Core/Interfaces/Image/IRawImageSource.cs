using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Core.Interfaces.Image
{
    public interface IRawImageSource
    {
        public int NumberOfLayers { get; }

        public int ImageWidth { get; }
        public int ImageHeight { get; }

        public double XResolution { get; set; }
        public double YResolution { get; set; }
        public double ZResolution { get; set; }
        public int DicomLevel { get; set; }
        public int DicomRange { get; set; }
        public Bitmap this[int idx] { get; }

        public int GetDicomPixelValue(int x, int y, int z);

        public IDictionary<int, Bitmap> ToDictionary();
    }
}
