using Core.Interfaces.Image;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Image
{
    public class BlobImageSource : IBlobImageSource
    {
        private readonly int _imageWidth;
        private readonly int _imageHeight;

        private readonly Dictionary<int, BlobImage> _blobImages;

        // TODO: validate blobimage index
        public BlobImage this[int idx] => _blobImages[idx];

        public BlobImageSource(int imageWidth, int imageHeight, int numOfLayers)
        {
            _imageWidth = imageWidth;
            _imageHeight = imageHeight;

            _blobImages = new Dictionary<int, BlobImage>();
            for (var i = 0; i < numOfLayers; ++i)
            {
                _blobImages.Add(i, new BlobImage(_imageWidth, _imageHeight));
            }
        }

        public BlobImageSource(IEnumerable<BlobImageEntity> blobImageEntities)
        {
            _blobImages = new Dictionary<int, BlobImage>();
            foreach (var blobImageEntity in blobImageEntities)
            {
                _blobImages.Add(blobImageEntity.LayerIndex, new BlobImage(blobImageEntity));
            }
        }

        public IDictionary<int, BlobImage> ToDictionary()
        {
            return new Dictionary<int, BlobImage>(_blobImages);
        }

        public IEnumerable<BlobImageEntity> GetBlobImageEntities()
        {
            var blobImageEntities = new List<BlobImageEntity>();
            foreach (var blobImage in _blobImages)
            {
                var blobImageEntity = blobImage.Value.ToBlobImageEntity();
                blobImageEntity.LayerIndex = blobImage.Key;
                blobImageEntities.Add(blobImageEntity);
            }
            return blobImageEntities;
        }
    }
}
