using Core.Image;
using Core.Interfaces.Image;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark.Operation
{
    public class OperationUnderTestBase
    {
        
        public IDictionary<int, IBlobImage> BlobImages = new Dictionary<int, IBlobImage>();
        public IDictionary<int, Bitmap> RawImages = new Dictionary<int, Bitmap>();

        public OperationUnderTestBase()
        {
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.Parent.Parent.Parent.Parent.FullName;
            string dataDirectory = Path.Join(projectDirectory, @"\data");
            var reader = new DicomReader(dataDirectory);

            for (int i = 0; i < reader.NumberOfLayers; i++)
            {
                RawImages.Add(i, reader[i]);
                BlobImages.Add(i, new BlobImage(512, 512));
            }
        }
    }
}
