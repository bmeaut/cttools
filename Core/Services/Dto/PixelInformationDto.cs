using Core.Image;
using Core.Interfaces.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.Dto
{
    public class PixelInformationDto
    {
        public int DicomValue { get; set; }

        public byte RawImageValue { get; set; }

        public int BlobId { get; set; }

        public IEnumerable<ITag<int>> Tags { get; set; }
    }
}
