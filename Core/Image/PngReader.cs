using Core.Interfaces.Image;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Core.Image
{
    public class PngReader : IRawImageSource
    {
        private Bitmap[] images;

        public PngReader(string path)
        {
            var filePaths = Directory.GetFiles(path, "*.png")
                .Select(f => Path.GetFullPath(f));
            images = filePaths.Select(f => new Bitmap(f)).ToArray();
        }

        public PngReader(string[] files)
        {
            var filePaths = files.Select(f => Path.GetFullPath(f));

            images = new Bitmap[filePaths.ToList().Count];
            int i = 0;
            foreach (var filePath in filePaths)
            {
                using (Bitmap bm = new Bitmap(filePath))
                {
                    images[i] = new Bitmap(bm);
                }
                i++;
            }
            //images = filePaths.Select(f => new Bitmap(f)).ToArray();
        }

        public Bitmap this[int idx] => images[idx];

        public int NumberOfLayers => images.Length;

        public int ImageWidth => images[0].Width;

        public int ImageHeight => images[0].Height;

        public double XResolution { get; set; }
        public double YResolution { get; set; }
        public double ZResolution { get; set; }
        public int DicomLevel { get; set; }
        public int DicomRange { get; set; }

        public int GetDicomPixelValue(int x, int y, int z)
        {
            return 0;
        }

        public IDictionary<int, Bitmap> ToDictionary()
        {
            var result = new Dictionary<int, Bitmap>();

            for (var i = 0; i < NumberOfLayers; ++i)
            {
                result.Add(i, this[i]);
            }

            return result;
        }
    }
}
