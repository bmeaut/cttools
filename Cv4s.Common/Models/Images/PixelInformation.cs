using Cv4s.Common.Interfaces.Images;

namespace Cv4s.Common.Models.Images
{
    public class PixelInformation
    {
        public int DicomValue { get; set; }

        public byte RawImageValue { get; set; }

        public int BlobId { get; set; }
    }
}
