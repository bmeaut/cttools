using Core.Enums;
using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation;
using Core.Operation.InternalOutputs;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;
using static Core.Operation.DistanceTransformOperation;

namespace Core.Test.Operation
{
    public class DistanceTransformOperationTests
    {
        private readonly DistanceTransformOperation op = new DistanceTransformOperation();
        private readonly OperationContext context;
        private readonly Mat[] binaryTestImages;
        private readonly CancellationToken token = new CancellationToken();
        private readonly MaterialTags material = MaterialTags.CEMENT;

        public DistanceTransformOperationTests()
        {
            int layerCount = 5;
            context = new OperationContext();

            context.BlobImages = new Dictionary<int, IBlobImage>();
            binaryTestImages = new Mat[layerCount];
            Random random = new Random(4214213);
            for (int i = 0; i < layerCount; i++)
            {
                var image = new BlobImage(33, 44);
                var mask = GenerateMaskWithRandomPoints(image, random);
                binaryTestImages[i] = mask;
                AddBlobsWithTagsToImage(image, mask);
                context.BlobImages.Add(i, image);
            }

            context.ActiveLayer = 0;
            context.OperationProperties = new DistanceTransformOperationProperties() { ForegroundMaterial = material };
            context.RawImageMetadata = new RawImageMetadata()
            {
                XResolution = 0.6,
                YResolution = 0.6,
                ZResolution = 1.4
            };
        }

        [Fact]
        public async void ResultsAreSameAsBruteForceAlgorithmResults()
        {
            int layerCount = context.BlobImages.Count;
            int width = context.ActiveBlobImage.Size.Width;
            int height = context.ActiveBlobImage.Size.Height;

            var resultContext = await op.Run(context, new Progress<double>(), token);
            resultContext.InternalOutputs.TryGetValue(DistanceTransformOperation.OutputName, out InternalOutput dtOutput);
            Mat[] operationResult = (dtOutput as MatOutput).Values;

            Mat[] bruteForceResult = DistanceTransformBruteForce(
                binaryTestImages,
                context.RawImageMetadata.ZResolution,
                context.RawImageMetadata.XResolution);

            var opIndexers = new Mat.Indexer<float>[layerCount];
            var bfIndexers = new Mat.Indexer<float>[layerCount];
            for (int i = 0; i < layerCount; i++)
            {
                opIndexers[i] = operationResult[i].GetGenericIndexer<float>();
                bfIndexers[i] = bruteForceResult[i].GetGenericIndexer<float>();
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < layerCount; z++)
                    {
                        Assert.Equal(bfIndexers[z][y, x], opIndexers[z][y, x], 3);
                    }
                }
            }
        }

        private Mat GenerateMaskWithRandomPoints(IBlobImage image, Random random)
        {
            Mat mask = image.GetEmptyMask();
            var indexer = mask.GetGenericIndexer<byte>();
            for (int x = 0; x < mask.Width; x++)
            {
                for (int y = 0; y < mask.Height; y++)
                {
                    indexer[y, x] = (byte)(random.Next(500) == 0 ? 0 : 255);
                }
            }
            return mask;
        }

        private void AddBlobsWithTagsToImage(IBlobImage image, Mat mask)
        {
            int id = image.GetNextUnusedBlobId();
            image.SetBlobMask(mask, id);
            image.SetTag(id, material.ToString());
        }

        private Mat[] DistanceTransformBruteForce(Mat[] src, double layerDistance, double pixelDistance)
        {
            int width = src[0].Width;
            int height = src[0].Height;
            int layerCount = src.Length;

            Mat[] dt = new Mat[layerCount];
            for (int i = 0; i < layerCount; i++)
            {
                dt[i] = new Mat(height, width, MatType.CV_32F);
            }

            var srcIndexers = new Mat.Indexer<byte>[layerCount];
            var dtIndexers = new Mat.Indexer<float>[layerCount];
            for (int i = 0; i < layerCount; i++)
            {
                srcIndexers[i] = src[i].GetGenericIndexer<byte>();
                dtIndexers[i] = dt[i].GetGenericIndexer<float>();
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < layerCount; z++)
                    {
                        dtIndexers[z][y, x] = float.MaxValue;
                    }
                }
            }
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < layerCount; z++)
                    {
                        {
                            if (srcIndexers[z][y, x] == 0)
                            {
                                dtIndexers[z][y, x] = 0;
                            }
                            else
                            {
                                for (int i = 0; i < width; i++)
                                {
                                    for (int j = 0; j < height; j++)
                                    {
                                        for (int k = 0; k < layerCount; k++)
                                        {

                                            if (srcIndexers[k][j, i] == 0)
                                            {
                                                float weight = (float)(layerDistance / pixelDistance);
                                                float distance = (float)Math.Sqrt((i - x) * (i - x) + (j - y) * (j - y) + (k - z) * (k - z) * weight * weight);
                                                distance = (float)(distance * pixelDistance);
                                                if (distance < dtIndexers[z][y, x])
                                                {
                                                    dtIndexers[z][y, x] = distance;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return dt;
        }
    }
}
