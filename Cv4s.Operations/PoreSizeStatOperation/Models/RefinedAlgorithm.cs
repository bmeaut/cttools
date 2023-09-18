using Cv4s.Common.Interfaces.Images;
using OpenCvSharp;

namespace Cv4s.Operations.PoreSizeStatOperation.Models
{
    public class RefinedAlgorithm : IPoreSizeAlgorithm
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

                List<Point3d> points = Get3dPointsForPore(onePore.Value, blobImages, ResX, ResY, ResZ);

                var degree = 30;
                var diameter = GetMaximumDiameter(points, degree);
                //if(diameter < ResZ) {   // if the diameter is smaller than the distance between layers, the layer distance is counted as diameter
                //    diameter = ResZ;
                //}
                poreSizes[onePore.Key].Size = diameter;
                //var sphereVolume = 4.0 / 3.0 * Math.Pow(diameter/2.0, 3) * Math.PI;    // this is a maximum, when the pore is a sphere
                var pixelCount = CountPixelsForPoint(onePore.Value, blobImages);
                var pixelVolume = pixelCount * ResX * ResY * ResZ;
                poreSizes[onePore.Key].Volume = pixelVolume;
            }

            return poreSizes;
        }

        private int CountPixelsForPoint(List<LayerPore> layerPores, IBlobImageSource blobImages)
        {
            var pixelCount = 0;
            foreach (var layerPore in layerPores)
            {
                Mat image = blobImages[layerPore.layerId].GetMask(layerPore.blobId);
                pixelCount += image.CountNonZero();
            }

            return pixelCount;
        }

        private double GetMaximumDiameter(List<Point3d> points, int degree)
        {
            var diameter = 0.0;

            for (int i = 0; i < 180; i += degree)
            { // only 180 degrees because it doesnt matter which direction you're calculating the diameter from
                var xRotationMatrix = Calculate3dRotationMatrixXaxis(i / 180.0 * Math.PI);
                for (int j = 0; j <= 90; j += degree)
                { // only 90 degrees because while rotating in 3d on both axes you get the same diameter (see geogebra)
                    // checks every possible angle combination
                    var yRotationMatrix = Calculate3dRotationMatrixYaxis(j / 180.0 * Math.PI);
                    var maxZCoordinate = Double.NegativeInfinity;
                    var minZCoordinate = Double.PositiveInfinity;
                    foreach (var point in points)
                    {
                        // checks every possible point in the angle combinations
                        var transformedPoint = PointAndMatrixMultiplication(xRotationMatrix, point);
                        transformedPoint = PointAndMatrixMultiplication(yRotationMatrix, transformedPoint);
                        if (transformedPoint.Z < minZCoordinate)
                        {
                            minZCoordinate = transformedPoint.Z;
                        }
                        if (transformedPoint.Z > maxZCoordinate)
                        {
                            maxZCoordinate = transformedPoint.Z;
                        }
                    }
                    if (maxZCoordinate - minZCoordinate > diameter)
                    {
                        diameter = maxZCoordinate - minZCoordinate;
                    }
                }
            }
            return diameter;
        }

        private Point3d PointAndMatrixMultiplication(Mat matrix, Point3d point)
        {
            return new Point3d(
                matrix.Get<Double>(0, 0) * point.X + matrix.Get<Double>(0, 1) * point.Y + matrix.Get<Double>(0, 2) * point.Z,
                matrix.Get<Double>(1, 0) * point.X + matrix.Get<Double>(1, 1) * point.Y + matrix.Get<Double>(1, 2) * point.Z,
                matrix.Get<Double>(2, 0) * point.X + matrix.Get<Double>(2, 1) * point.Y + matrix.Get<Double>(2, 2) * point.Z
                );
        }

        private Mat Calculate3dRotationMatrixXaxis(double radian)
        {
            Mat rotationMatrixOn = new Mat(3, 3, MatType.CV_64F);
            rotationMatrixOn.Set<Double>(0, 0, 1.0);
            rotationMatrixOn.Set<Double>(0, 1, 0.0);
            rotationMatrixOn.Set<Double>(0, 2, 0.0);
            rotationMatrixOn.Set<Double>(1, 0, 0.0);
            rotationMatrixOn.Set<Double>(1, 1, Math.Cos(radian));
            rotationMatrixOn.Set<Double>(1, 2, -Math.Sin(radian));
            rotationMatrixOn.Set<Double>(2, 0, 0.0);
            rotationMatrixOn.Set<Double>(2, 1, Math.Sin(radian));
            rotationMatrixOn.Set<Double>(2, 2, Math.Cos(radian));

            return rotationMatrixOn;
        }

        private Mat Calculate3dRotationMatrixYaxis(double radian)
        {
            Mat rotationMatrix = new Mat(3, 3, MatType.CV_64F);
            rotationMatrix.Set<Double>(0, 0, Math.Cos(radian));
            rotationMatrix.Set<Double>(0, 1, 0.0);
            rotationMatrix.Set<Double>(0, 2, Math.Sin(radian));
            rotationMatrix.Set<Double>(1, 0, 0.0);
            rotationMatrix.Set<Double>(1, 1, 1.0);
            rotationMatrix.Set<Double>(1, 2, 0.0);
            rotationMatrix.Set<Double>(2, 0, -Math.Sin(radian));
            rotationMatrix.Set<Double>(2, 1, 0.0);
            rotationMatrix.Set<Double>(2, 2, Math.Cos(radian));

            return rotationMatrix;
        }

        private List<Point3d> Get3dPointsForPore(List<LayerPore> layerPores, IBlobImageSource blobImages, double ResX, double ResY, double ResZ)
        {
            List<Point3d> points = new List<Point3d>();
            foreach (var layerPore in layerPores)
            {
                Mat image = blobImages[layerPore.layerId].GetMask(layerPore.blobId);
                Cv2.FindContours(image, out contourPoints, out indexes, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                if (contourPoints.Length == 0)
                {
                    continue;
                }
                else
                {
                    foreach (var point in contourPoints[0])
                    {
                        points.Add(new Point3d(point.X * ResX, point.Y * ResY, layerPore.layerId * ResZ));    // transforms the pixel coordinates to real geometry
                    }
                }
            }
            return points;
        }

    }
}
