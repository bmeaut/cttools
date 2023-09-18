using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Operation
{
    public class RawImageMetadata
    {
        public int ImageWidth { get; set; }

        public int ImageHeight { get; set; }

        public int NumberOfLayers { get; set; }

        public double XResolution { get; set; }
        public double YResolution { get; set; }
        public double ZResolution { get; set; }

        public IEnumerable<string> RawImagePaths { get; set; }
    }
}
