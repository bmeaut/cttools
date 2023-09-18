using Core.Interfaces.Image;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces.Operation
{
    // Images, files attached to a measurement
    public interface IArtifact
    {
        public IEnumerable<IBlobImage> BlobImageChangeSet { get; set; }
    }
}
