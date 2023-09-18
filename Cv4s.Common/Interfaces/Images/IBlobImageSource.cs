using Cv4s.Common.Models.Images;
using static System.Collections.Generic.Dictionary<int, Cv4s.Common.Models.Images.BlobImage>;

namespace Cv4s.Common.Interfaces.Images
{
    public interface IBlobImageSource
    {
        public int ImageWidth { get; }
        public int ImageHeight { get; }
        public int NumOfLayers { get; }

        public BlobImage this[int idx] { get; set; }

        public IDictionary<int, BlobImage> ToDictionary();

        public KeyCollection Keys { get; }

        public ValueCollection Values { get; }
    }
}
