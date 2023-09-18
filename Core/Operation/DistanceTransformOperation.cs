using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation.InternalOutputs;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Core.Enums;

namespace Core.Operation
{
    public class DistanceTransformOperation : IOperation
    {
        public static readonly string OutputName = "DistanceValues";

        public string Name => "Distance transform";

        public OperationProperties DefaultOperationProperties => new DistanceTransformOperationProperties();

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            int layerCount = context.BlobImages.Count;
            var properties = (DistanceTransformOperationProperties)context.OperationProperties;
            var material = properties.ForegroundMaterial.ToString();
            var _binaryImages = new Mat[layerCount];

            Parallel.For(0, layerCount, (i, state) =>
            {
                var blobImage = context.BlobImages[i];
                _binaryImages[i] = GetBinaryImageFromBlobImage(blobImage, material);
            });

            double layerDistance = context.RawImageMetadata.ZResolution;
            double pixelDistance = context.RawImageMetadata.XResolution;
            Mat[] distanceValues = EucledianDistanceTransform3D(_binaryImages, layerDistance, pixelDistance);
            //Mat test = new Mat(distanceValues[0].Size(), MatType.CV_8UC1);
            //Cv2.Normalize(distanceValues[0], test, 0, 1, NormTypes.MinMax);
            //Cv2.ImShow("dt", test);
            //Cv2.ImShow("binary", _binaryImages[0]);
            //Cv2.WaitKey();

            var internalOutput = new MatOutput() { Values = distanceValues };
            context.AddInternalOutput(OutputName, internalOutput);

            return context;
        }

        private Mat GetBinaryImageFromBlobImage(IBlobImage blobImage, string material)
        {
            var foregroundBlobIds = blobImage.GetBlobsByTagValue(material, null);
            return blobImage.GetMaskUnion(foregroundBlobIds);
        }

        private MatType type = MatType.CV_32F;

        private Mat[] EucledianDistanceTransform3D(Mat[] src, double layerDistance, double pixelDistance)
        {
            var width = src[0].Width;
            var height = src[0].Height;
            var layerCount = src.Length;

            Mat[] dst = new Mat[layerCount];
            for (int i = 0; i < layerCount; i++)
            {
                dst[i] = new Mat(height, width, type);
            }

            Mat[] dtImages2D = new Mat[layerCount];

            for (int i = 0; i < layerCount; i++)
            {
                Mat dtLayer = new Mat();
                Cv2.DistanceTransform(src[i], dtLayer, DistanceTypes.L2, DistanceTransformMasks.Precise, type);
                dtImages2D[i] = dtLayer;
            }

            RowIndexer dstRow, srcRow;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    srcRow = new RowIndexer(dtImages2D, x, y);
                    dstRow = new RowIndexer(dst, x, y);
                    EucledianDistanceTransform1D(srcRow, dstRow, (float)layerDistance, (float)pixelDistance);
                }

            return dst;
        }

        private void EucledianDistanceTransform1D(RowIndexer f, RowIndexer dt, float layerDistance, float pixelDistance)
        {
            float weight = layerDistance / pixelDistance;
            int[] v = new int[f.Length];
            float[] z = new float[f.Length + 1];

            int k = 0;
            v[0] = 0;
            z[0] = float.MinValue;
            z[1] = float.MaxValue;
            for (int q = 1; q <= f.Length - 1; q++)
            {
                float s = (Square(weight) * (Square(q) - Square(v[k])) + Square(f[q]) - Square(f[v[k]])) / (Square(weight) * 2 * (q - v[k]));
                while (s <= z[k])
                {
                    k--;
                    s = (Square(weight) * (Square(q) - Square(v[k])) + Square(f[q]) - Square(f[v[k]])) / (Square(weight) * 2 * (q - v[k]));
                }
                k++;
                v[k] = q;
                z[k] = s;
                z[k + 1] = float.MaxValue;
            }
            k = 0;
            for (int q = 0; q <= f.Length - 1; q++)
            {
                while (z[k + 1] < q)
                    k++;
                dt[q] = (float)Math.Sqrt(Square(weight) * Square(q - v[k]) + Square(f[v[k]])) * pixelDistance;
            }
        }

        private static float Square(float a)
        {
            return a * a;
        }

        class RowIndexer
        {
            private readonly Mat[] images;
            private readonly Mat.Indexer<float>[] indexers;
            private readonly int x, y;

            public int Length => images.Length;
            public RowIndexer(Mat[] images, int x, int y)
            {
                this.images = images;
                indexers = new Mat.Indexer<float>[images.Length];
                for (int i = 0; i < this.images.Length; i++)
                {
                    indexers[i] = this.images[i].GetGenericIndexer<float>();
                }
                this.x = x;
                this.y = y;
            }

            private float GetValue(int index)
            {
                return indexers[index][y, x];
            }

            private void SetValue(int index, float value)
            {
                indexers[index][y, x] = value;
            }

            public float this[int index]
            {
                get => GetValue(index);
                set => SetValue(index, value);
            }
        }

        public class DistanceTransformOperationProperties : OperationProperties
        {
            public MaterialTags ForegroundMaterial { get; set; } = MaterialTags.CEMENT;
        }
    }
}
