using Core.Enums;
using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class RoundnessShapeClassifierOperation : MultiLayerParallelOperationBase
    {
        public override string Name => "RoundnessShapeClassifier";

        public static string RoundnessTagName = "Roundness";

        public override OperationProperties DefaultOperationProperties => new RoundnessShapeClassifierOperationProperties();
        private object masksLock = new object();
        private object doneLock = new object();

        public override async Task<OperationContext> RunOneLayer(OperationContext context, int layer, IProgress<double> progress, CancellationToken token)
        {
            var blobImage = context.BlobImages[layer];
            var properties = context.OperationProperties as RoundnessShapeClassifierOperationProperties;
            var material = properties.Material.ToString();
            IEnumerable<int> materialBlobs = null;
            Dictionary<int, Rect> boundingRects;

            materialBlobs = blobImage.GetBlobsByTagValue(material, null);
            boundingRects = GetBoundingRects(blobImage, materialBlobs);

            int done = 0;
            Dictionary<int, Mat> masks = new Dictionary<int, Mat>();
            Parallel.ForEach(materialBlobs, blob =>
            {
                Mat blobMask = GetBlobMask(blobImage, blob, boundingRects[blob]);
                lock (masksLock) masks.Add(blob, blobMask);
            });
            foreach (var blob in masks.Keys)
            {
                var blobMask = masks[blob];
                Cv2.FindContours(blobMask, out Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.CComp, ContourApproximationModes.ApproxNone);
                double area = 0;
                double circumference = 0;
                for (int i = 0; i < contours.Length; i++)
                {
                    var contourArea = Cv2.ContourArea(contours[i]);

                    if (hierarchy[i].Parent < 0)
                        area += contourArea;
                    else
                        area -= contourArea;
                    circumference += Cv2.ArcLength(contours[i], true);
                }
                double roundness;
                if (area == 0 || circumference == 0)
                    roundness = 1;
                else
                    roundness = 4 * Math.PI * area / (circumference * circumference);

                blobImage.SetTag(blob, RoundnessTagName, (int)Math.Round(roundness * 1000));

                lock (doneLock) done++;
                progress.Report(done / (double)materialBlobs.Count());
                if (token.IsCancellationRequested)
                {
                    return context;
                }
            }

            return context;
        }

        private Dictionary<int, Rect> GetBoundingRects(IBlobImage blobImage, IEnumerable<int> materialBlobs)
        {
            var boundingRects = new Dictionary<int, Rect>();
            foreach (var blob in materialBlobs)
            {
                boundingRects.Add(blob, new Rect(new Point(0, 0), blobImage.Size));
            }

            for (int x = 0; x < blobImage.Size.Width; x++)
            {
                for (int y = 0; y < blobImage.Size.Height; y++)
                {
                    var blob = blobImage[y, x];
                    if (boundingRects.ContainsKey(blob))
                    {
                        var boundingRect = boundingRects[blob];
                        if (!boundingRect.Contains(new Point(x, y)))
                        {
                            int minX = boundingRect.X;
                            int minY = boundingRect.Y;
                            int width = boundingRect.Width;
                            int height = boundingRect.Height;
                            if (x < boundingRect.X)
                            {
                                width += (boundingRect.X - x);
                                minX = x;
                            }
                            else if (x > boundingRect.X + boundingRect.Width - 1)
                            {
                                width = x - boundingRect.X;
                            }
                            if (y < boundingRect.Y)
                            {
                                height += (boundingRect.Y - y);
                                minY = y;
                            }
                            else if (y > boundingRect.Y + boundingRect.Height - 1)
                            {
                                height = y - boundingRect.Y;
                            }
                            boundingRects[blob] = new Rect(minX, minY, width, height);
                        }
                    }
                }
            }
            return boundingRects;
        }

        private Mat GetBlobMask(IBlobImage blobImage, int blob, Rect roi)
        {
            var mask = new Mat(roi.Size, MatType.CV_8UC1, 0);
            var maskIndexer = mask.GetGenericIndexer<byte>();
            for (int x = roi.X; x < roi.Right; x++)
            {
                for (int y = roi.Y; y < roi.Bottom; y++)
                {
                    if (blobImage[y, x] == blob)
                    {
                        maskIndexer[y, x] = 255;
                    }
                }
            }
            return mask;
        }
    }
    public class RoundnessShapeClassifierOperationProperties : MultiLayerOperationProtertiesBase
    {
        public MaterialTags Material { get; set; }

    }
}
