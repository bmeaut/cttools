using Core.Image;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Core.Test.Image
{
    public class DicomReaderTests
    {
        private readonly string dataDirectory;

        public DicomReaderTests()
        {
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            dataDirectory = projectDirectory + @"\data";
        }

        [Fact]
        public void ReadFromEmptyDirectory()
        {
            Assert.Throws<DirectoryNotFoundException>(
                () => new DicomReader(@"nonexistentDirectory"));
        }

        [Fact]
        public void ReadDicomFiles()
        {
            var reader = new DicomReader(dataDirectory);
            Assert.Equal(2, reader.NumberOfLayers);

            Mat layer0 = BitmapConverter.ToMat(reader[0]);
            Assert.Equal(512, layer0.Width);
            Assert.Equal(512, layer0.Height);
            Assert.Equal(MatType.CV_8UC3, layer0.Type());

            Assert.Equal(0.3164, reader.XResolution, 4);
            Assert.Equal(0.3164, reader.YResolution, 4);
            Assert.Equal(42, reader.ZResolution, 4);

            Assert.NotNull(reader[reader.NumberOfLayers - 1]);
            Assert.Throws<KeyNotFoundException>(() => reader[reader.NumberOfLayers]);
        }

        [Fact]
        public void ReadOneDicomFileOnly()
        {
            string[] files = new string[1];
            files[0] = Path.Join(dataDirectory, "IM-0001-0005.dcm");
            var reader = new DicomReader(files);
            Assert.Equal(0, reader.ZResolution, 1);
            Assert.Equal(1, reader.NumberOfLayers);
        }
    }
}
