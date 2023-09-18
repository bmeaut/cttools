using Cv4s.Common.Interfaces.Images;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cv4s.Common.Models.Images
{
    public class BlobImageSource : IBlobImageSource
    {
        private readonly int _imageWidth;
        private readonly int _imageHeight;

        private readonly Dictionary<int, BlobImage> _blobImages = new Dictionary<int, BlobImage>();

        public int ImageWidth => _imageWidth;

        public int ImageHeight => _imageHeight;

        public int NumOfLayers => _blobImages.Count;

        public Dictionary<int, BlobImage>.KeyCollection Keys => _blobImages.Keys;

        public Dictionary<int, BlobImage>.ValueCollection Values => _blobImages.Values;

        BlobImage IBlobImageSource.this[int idx]
        {
            get => _blobImages[idx];

            set => _blobImages[idx] = value;
        }

        public BlobImageSource(int imageWidth, int imageHeight, int numOfLayers)
        {
            _imageWidth = imageWidth;
            _imageHeight = imageHeight;

            InitBlobImages(numOfLayers);
        }

        public BlobImageSource(IRawImageSource imageSource)
        {
            _imageHeight = imageSource.ImageHeight;
            _imageWidth = imageSource.ImageWidth;

            InitBlobImages(imageSource.NumOfLayers);
        }

        public IDictionary<int, BlobImage> ToDictionary()
        {
            return new Dictionary<int, BlobImage>(_blobImages);
        }

        private void InitBlobImages(int layers)
        {
            _blobImages.Clear();

            for (var i = 0; i < layers; ++i)
            {
                _blobImages.Add(i, new BlobImage(_imageWidth, _imageHeight));
            }
        }
    }
}
