using Cv4s.Common.Enums;
using System.Drawing;

namespace Cv4s.Common.Interfaces.Images
{
    public interface IRawImageSource
    {
        public int ImageWidth { get; }
        public int ImageHeight { get; }
        public int NumOfLayers { get; }

        public double XResolution { get; set; }
        public double YResolution { get; set; }
        public double ZResolution { get; set; }


        public int DicomLevel { get; set; }
        public int DicomRange { get; set; }

        public string[] RawImagePaths { get; set; }

        public Bitmap this[int idx] { get; }

        ScanFileFormat ScanFileFormat { get; }

        public int GetDicomPixelValue(int x, int y, int z);

        public IDictionary<int, Bitmap> ToDictionary();
    }
}
