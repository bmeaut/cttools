using Core.Interfaces.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Operation.OperationTools
{
    public class ImagePlane
    {
        private readonly IDictionary<int, IBlobImage> images;

        private readonly int[] indices = new int[3];

        private readonly int index1Offset;

        private readonly int index2Offset;

        public int Width { get; }

        public int Heigth { get; }

        public enum Direction
        {
            X,
            Y,
            Z
        }

        public ImagePlane(IDictionary<int, IBlobImage> images, Direction direction, int index)
        {
            this.images = images;
            switch (direction)
            {
                case Direction.X:
                    indices[0] = index;
                    index1Offset = 1;
                    index2Offset = 2;
                    Width = images[0].Size.Height;
                    Heigth = images.Count;
                    break;
                case Direction.Y:
                    indices[1] = index;
                    index1Offset = 0;
                    index2Offset = 2;
                    Width = images[0].Size.Width;
                    Heigth = images.Count;
                    break;
                case Direction.Z:
                    indices[2] = index;
                    index1Offset = 0;
                    index2Offset = 1;
                    Width = images[0].Size.Width;
                    Heigth = images[0].Size.Height;
                    break;
            }
        }

        public int GetBlobIdAt(int index1, int index2)
        {
            SetIndices(index1, index2);
            return images[indices[2]][indices[1], indices[0]];
        }

        public IEnumerable<ITag<int>> GetTagsAt(int index1, int index2)
        {
            SetIndices(index1, index2);
            var image = images[indices[2]];
            var blobId = image[indices[1], indices[0]];
            return image.GetTagsForBlob(blobId);
        }

        private void SetIndices(int index1, int index2)
        {
            indices[index1Offset] = index1;
            indices[index2Offset] = index2;
        }
    }
}
