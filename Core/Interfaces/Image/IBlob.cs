using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces.Image
{
    public interface IBlob
    {
        public int BlobId { get; set; }

        public IEnumerable<ITag<int>> Tags { get; set; }
    }
}
