using Core.Image;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces.Image
{
    public interface IBlobImageSource
    {
        public BlobImage this[int idx] { get; }

        public IDictionary<int, BlobImage> ToDictionary();

        public IEnumerable<BlobImageEntity> GetBlobImageEntities();
    }
}
