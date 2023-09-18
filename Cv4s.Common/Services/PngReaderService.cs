using Cv4s.Common.Enums;
using Cv4s.Common.Interfaces.Images;
using System.Drawing;

namespace Cv4s.Common.Services
{
    public class PngReaderService : IRawImageSource
    {
        private Bitmap[] _images;

        public Bitmap this[int idx] => _images[idx];

        public int ImageWidth => _images[0].Width;

        public int ImageHeight => _images[0].Height;

        public double XResolution { get; set; }
        public double YResolution { get; set; }
        public double ZResolution { get; set; }
        public int DicomLevel { get; set; } = 0;
        public int DicomRange { get; set; } = 0;

        public int NumOfLayers => _images.Length;

        public ScanFileFormat ScanFileFormat => ScanFileFormat.PNG;

        public string[] RawImagePaths { get; set; }

        public PngReaderService(string path)
        {
            var filePaths = Directory.GetFiles(path, "*.png")
                .Select(f => Path.GetFullPath(f));
            _images = filePaths.Select(f => new Bitmap(f)).ToArray();

            RawImagePaths = filePaths.ToArray();
        }

        public PngReaderService(string[] files)
        {
            var filePaths = files.Select(f => Path.GetFullPath(f));

            _images = new Bitmap[filePaths.ToList().Count];
            int i = 0;
            foreach (var filePath in filePaths)
            {
                using (Bitmap bm = new Bitmap(filePath))
                {
                    _images[i] = new Bitmap(bm);
                }
                i++;
            }

            RawImagePaths = filePaths.ToArray();
        }

        public PngReaderService(string[] paths, double x, double y, double z) : this(paths)
        {
            XResolution = x;
            YResolution = y;
            ZResolution = z;
        }

        public int GetDicomPixelValue(int x, int y, int z) => 0;

        public IDictionary<int, Bitmap> ToDictionary()
        {
            var result = new Dictionary<int, Bitmap>();

            for (var i = 0; i < NumOfLayers; ++i)
            {
                result.Add(i, this[i]);
            }

            return result;
        }
    }
}
