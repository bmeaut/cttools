using Cv4s.Common.Interfaces.Images;
using OpenCvSharp;

namespace Cv4s.Operations.PoreSizeStatOperation.Models
{
    public class SimpleAlgorithm : IPoreSizeAlgorithm
    {
        private Point[][] contourPoints;
        private HierarchyIndex[] indexes;
        private List<Point> hull = new List<Point>();
        private Dictionary<int, SizeVolume> poreSizes = new Dictionary<int, SizeVolume>();

        /// <summary>
        /// Calculates the size of the given <paramref name="poresOnEachLayer"/> using the <paramref name="blobImages"/> and the resolution ratios.
        /// The algorithm works like if we want to enbox each pore into the smallest box.
        /// After that we select the biggest side lenght and it will be the smallest sieve size which the pore can fit through
        /// </summary>
        /// <param name="poresOnEachLayer">All connected pores in a list, and its in a dictionary with Grain/Sieves id as a key</param>
        /// <param name="blobImages">the images that contains the pores and blobs</param>
        /// <param name="token">token to determine if the operation is cancelleíd</param>
        /// <param name="progress">for progress report</param>
        /// <param name="ResX">Size pixel ratio</param>
        /// <param name="ResY">Size pixel ratio</param>
        /// <param name="ResZ">Size pixel ratio</param>
        /// <returns></returns>
        public Dictionary<int, SizeVolume> Calculate(Dictionary<int, List<LayerPore>> poresOnEachLayer, IBlobImageSource blobImages, double ResX, double ResY, double ResZ)
        {
            var areaApproximation = ResX * ResY * ResZ;

            foreach (var onePore in poresOnEachLayer)
            {
                poreSizes.Add(onePore.Key, new SizeVolume() { Size = 0, Volume = 0 });

                double V = 0;
                double BoundingBoxX = 0;
                double BoundingBoxY = 0;
                double BoundingBoxZ = 0;

                foreach (var onePoreLayer in onePore.Value)
                {
                    Mat image = blobImages[onePoreLayer.layerId].GetMask(onePoreLayer.blobId);
                    Cv2.FindContours(image, out contourPoints, out indexes, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    //ha nem talal konturt akkor nem is letezik
                    if (contourPoints.Length == 0 || onePoreLayer.processed)
                        continue;

                    var approx = Cv2.ApproxPolyDP(contourPoints[0], 2, true);
                    var rect = Cv2.BoundingRect(approx);
                    var x = rect.Width * ResX;
                    var y = rect.Height * ResY;
                    V += (rect.Width * rect.Height * areaApproximation);

                    BoundingBoxX = BoundingBoxX < x ? x : BoundingBoxX;
                    BoundingBoxY = BoundingBoxY < y ? y : BoundingBoxY;
                }
                BoundingBoxZ = onePore.Value.Select(x => x.layerId).Distinct().Count() * ResZ;
                poreSizes[onePore.Key].Size = GetMinimumSieveSize(BoundingBoxX, BoundingBoxY, BoundingBoxZ);
                poreSizes[onePore.Key].Volume = V;
            }

            return poreSizes;
        }

        private double GetMinimumSieveSize(double x, double y, double z)
        {
            double max1 = x > y ? x : y;
            double max2 = x > z ? x : z;
            double max3 = y > z ? y : z;

            return new List<double> { max1, max2, max3 }.Min();
        }
    }
}
