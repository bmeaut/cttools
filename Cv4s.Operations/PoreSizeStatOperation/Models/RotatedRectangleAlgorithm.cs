using Cv4s.Common.Interfaces.Images;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cv4s.Operations.PoreSizeStatOperation.Models
{
    public class RotatedRectangleAlgorithm : IPoreSizeAlgorithm
    {
        private Point[][] contourPoints;
        private HierarchyIndex[] indexes;
        List<Point> hull = new List<Point>();
        Dictionary<int, SizeVolume> poreSizes = new Dictionary<int, SizeVolume>();

        public Dictionary<int, SizeVolume> Calculate(Dictionary<int, List<LayerPore>> poresOnEachLayer, IBlobImageSource blobImages, double ResX, double ResY, double ResZ)
        {
            foreach (var onePore in poresOnEachLayer)
            {
                poreSizes.Add(onePore.Key, new SizeVolume() { Size = 0, Volume = 0 });

                double V = 0;
                double S = 0;

                foreach (var onePoreLayer in onePore.Value)
                {
                    Mat image = blobImages[onePoreLayer.layerId].GetMask(onePoreLayer.blobId);
                    Cv2.FindContours(image, out contourPoints, out indexes, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                    // if it doesn't find any contours for it, it doesn't exist
                    if (contourPoints.Length == 0 || onePoreLayer.processed)
                        continue;

                    var approx = Cv2.ApproxPolyDP(contourPoints[0], 2, true);
                    var rect = Cv2.MinAreaRect(approx);
                    var box = Cv2.BoxPoints(rect);
                    if (box.Length != 4)
                    {
                        continue;
                    }
                    var width = CalculateRotatedScaledLength(box[0], box[1], ResX, ResY);
                    var height = CalculateRotatedScaledLength(box[1], box[2], ResX, ResY);
                    if (width == 0)
                    {
                        width = ResX * ResY * ResZ;
                    }
                    if (height == 0)
                    {
                        height = ResX * ResY * ResZ;
                    }
                    var size = width > height ? width : height;
                    V += (width * height * ResZ);
                    if (S < size)
                        S = size;

                }
                poreSizes[onePore.Key].Volume = V;
                poreSizes[onePore.Key].Size = S;
            }

            return poreSizes;
        }

        private double CalculateRotatedScaledLength(Point2f first, Point2f second, double ResX, double ResY)
        {
            var first_x = first.X;
            var first_y = first.Y;
            var second_x = second.X;
            var second_y = second.Y;

            var x_distance = (first_x - second_x) * ResX;
            var y_distance = (first_y - second_y) * ResY;

            var rotatedBase = Math.Sqrt(x_distance * x_distance +
                            y_distance * y_distance);

            return rotatedBase;
        }
    }
}
