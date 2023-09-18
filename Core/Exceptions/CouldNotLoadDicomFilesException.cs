using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Exceptions
{
    public class CouldNotLoadDicomFilesException : Exception
    {
        public List<string> Files { get; }

        public override string Message => $"Could not load some dicom files.";

        public CouldNotLoadDicomFilesException(List<string> files)
        {
            Files = files;
        }
    }
}
