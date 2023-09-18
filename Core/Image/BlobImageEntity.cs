using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Image
{
    public class BlobImageEntity : BaseEntity
    {
        public int LayerIndex { get; set; }

        public int[,] Image { get; set; }

        public Dictionary<int, List<Tag>> Tags { get; set; }
    }
}
