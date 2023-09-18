using Core.Enums;
using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation.InternalOutputs;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Core.Operation.OperationTools
{
    public abstract class PoreSizeStatOperationBase : IOperation
    {
        public virtual string Name => "PoreSizeStatOrepationBase";

        public virtual OperationProperties DefaultOperationProperties => new EmptyOperationProperties();//TODO megadni valaszthatonak az algo-t

        protected JsonResolution Resolutions;
        protected Dictionary<int, List<LayerPore>> allBlobIds = new Dictionary<int, List<LayerPore>>();
        protected Dictionary<double, double> Sieves = new Dictionary<double, double>(); //szitameretek es a tomegszazalekarany
        protected List<double> Percentages = new List<double>();
        protected Dictionary<int, SizeVolume> GrainSizesAndVolumes = new Dictionary<int, SizeVolume>();

        protected PoreSizeAlgorithm algorithm;
        protected delegate void SievesValueAdder(double key, SizeVolume pore);
        protected SievesValueAdder ValueAdder;

        protected string materialType = MaterialTags.PORE.ToString();

        public virtual async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            Sieves.Clear();
            GrainSizesAndVolumes.Clear();
            Percentages.Clear();
            allBlobIds.Clear();

            Resolutions = CollectResolutions(context);

            //Init sieves
            for (int i = 0; i < 12; i++) {
                var key = Math.Pow(2, i - 4);
                Sieves.Add(key, 0);
            }

            if (algorithm == null)
                //algorithm = new SimpleAlgorithm(); //TODO valaszthatonak
                //algorithm = new RotatedRectangleAlgorithm(); //TODO valaszthatonak
                algorithm = new RefinedAlgorithm();


            for (int i = 0; i < context.BlobImages.Count; i++) {                                                                     //list<Tag>
                List<int> poreBlobIds = context.BlobImages[i].GetBlobsByTagValue(materialType, null).ToList();
                poreBlobIds.Remove(0); //Removing not RealBlobIds

                foreach (int blobId in poreBlobIds)
                {
                    var poreIds = context.BlobImages[i].GetTagsForBlob(blobId).Where(t => t.Name.Equals(materialType)).Select(t => t.Value).ToList();
                    if (poreIds.Count > 1)
                        throw new Exception("Different tags for the same blob");

                    if (poreIds.Count != 0 && poreIds[0] >= 1) {
                        LayerPore pore = new LayerPore { layerId = i, blobId = blobId };
                        if (!allBlobIds.ContainsKey(poreIds[0]))
                            allBlobIds.Add(poreIds[0], new List<LayerPore>());
                        allBlobIds[poreIds[0]].Add(pore);
                    }
                }
            }

            progress.Report(0);
            GrainSizesAndVolumes = algorithm.Calculate(allBlobIds, context.BlobImages, token, progress,
                                            Resolutions.XResolution,
                                            Resolutions.YResolution,
                                            Resolutions.ZResolution);

            if (GrainSizesAndVolumes.Count == 0 || token.IsCancellationRequested)
                return context;

            foreach (var pore in GrainSizesAndVolumes.Select(s => s.Value).ToList()) {
                for (int i = 0; i < Sieves.Count - 1; i++) {
                    var prev = Sieves.ElementAt(i);
                    var next = Sieves.ElementAt(i + 1);
                    if (pore.Size >= prev.Key && pore.Size <= next.Key) {
                        ValueAdder?.Invoke(prev.Key, pore);
                        break;
                    }

                    if (i == (Sieves.Count - 2))//we are at the end of the dictionary and there is no place for our pore/greain
                        break;
                }
            }

            return context;
        }

        private JsonResolution CollectResolutions(OperationContext context) {
            var path = context.RawImageMetadata.RawImagePaths.First();
            var directory = Path.GetDirectoryName(path);
            var resFile = Path.Combine(directory, "resolutions.txt");
            if (File.Exists(resFile)) {
                string resolutions = File.ReadAllText(resFile);
                return JsonConvert.DeserializeObject<JsonResolution>(resolutions);
            }
            else {
                return new JsonResolution {
                    XResolution = context.RawImageMetadata.XResolution,
                    YResolution = context.RawImageMetadata.YResolution,
                    ZResolution = context.RawImageMetadata.ZResolution
                };
            }
        }
    }

    public class JsonResolution {
        public double XResolution;
        public double YResolution;
        public double ZResolution;
    }

    public class LayerPore {
        public int layerId;
        public int blobId;
        public bool processed = false;
    }

    public class SizeVolume {
        public double Size;
        public double Volume;
    }

    /// <summary>
    /// An interface, which needs to be implemented by the desired algorithm to determine the size of the grains/pores
    /// </summary>
    public interface PoreSizeAlgorithm
    {
        Dictionary<int, SizeVolume> Calculate(Dictionary<int, List<LayerPore>> poresOnEachLayer,
            IDictionary<int, IBlobImage> blobImages,
            CancellationToken token,
            IProgress<double> progress,
            double ResX,
            double ResY,
            double ResZ);
    }

    public class RotatedRectangleAlgorithm : PoreSizeAlgorithm {
        private Point[][] contourPoints;
        private HierarchyIndex[] indexes;
        List<Point> hull = new List<Point>();
        Dictionary<int, SizeVolume> poreSizes = new Dictionary<int, SizeVolume>();

        public Dictionary<int, SizeVolume> Calculate(
            Dictionary<int, List<LayerPore>> poresOnEachLayer,
            IDictionary<int, IBlobImage> blobImages,
            CancellationToken token,
            IProgress<double> progress,
            double ResX,
            double ResY,
            double ResZ) {
            int done = 0;
            double max = poresOnEachLayer.Keys.Count();
            try {
                foreach (var onePore in poresOnEachLayer) {
                    progress.Report(done / max);
                    poreSizes.Add(onePore.Key, new SizeVolume() { Size = 0, Volume = 0 });

                    if (token.IsCancellationRequested)
                        return poreSizes;

                    double V = 0;
                    double S = 0;
                    foreach (var onePoreLayer in onePore.Value) {
                        Mat image = blobImages[onePoreLayer.layerId].GetMask(onePoreLayer.blobId);
                        Cv2.FindContours(image, out contourPoints, out indexes, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                        // if it doesn't find any contours for it, it doesn't exist
                        if (contourPoints.Length == 0 || onePoreLayer.processed)
                            continue;

                        var approx = Cv2.ApproxPolyDP(contourPoints[0], 2, true);
                        var rect = Cv2.MinAreaRect(approx);
                        var box = Cv2.BoxPoints(rect);
                        if (box.Length != 4) {
                            continue;
                        }
                        var width = CalculateRotatedScaledLength(box[0], box[1], ResX, ResY);
                        var height = CalculateRotatedScaledLength(box[1], box[2], ResX, ResY);
                        if (width == 0) {
                            width = ResX * ResY * ResZ;
                        }
                        if (height == 0) {
                            height = ResX * ResY * ResZ;
                        }
                        var size = width > height ? width : height;
                        V += (width * height * ResZ);
                        if (S < size)
                            S = size;

                    }
                    poreSizes[onePore.Key].Volume = V;
                    poreSizes[onePore.Key].Size = S;
                    done++;
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
            return poreSizes;
        }
        private double CalculateRotatedScaledLength(Point2f first, Point2f second, double ResX, double ResY) {
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


    public class RefinedAlgorithm : PoreSizeAlgorithm {
        private Point[][] contourPoints;
        private HierarchyIndex[] indexes;
        List<Point> hull = new List<Point>();
        Dictionary<int, SizeVolume> poreSizes = new Dictionary<int, SizeVolume>();

        public Dictionary<int, SizeVolume> Calculate(
        Dictionary<int, List<LayerPore>> poresOnEachLayer,
        IDictionary<int, IBlobImage> blobImages,
        CancellationToken token,
        IProgress<double> progress,
        double ResX,
        double ResY,
        double ResZ) {
            int done = 0;
            double max = poresOnEachLayer.Keys.Count();
            var areaApproximation = ResX * ResY * ResZ;
            try {
                foreach (var onePore in poresOnEachLayer) {
                    progress.Report(done / max);
                    poreSizes.Add(onePore.Key, new SizeVolume() { Size = 0, Volume = 0 });

                    if (token.IsCancellationRequested)
                        return poreSizes;

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


                    done++;
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
            return poreSizes;
        }
        private int CountPixelsForPoint(List<LayerPore> layerPores, IDictionary<int, IBlobImage> blobImages) {
            var pixelCount = 0;
            foreach (var layerPore in layerPores) {
                Mat image = blobImages[layerPore.layerId].GetMask(layerPore.blobId);
                pixelCount += image.CountNonZero();
            }

            return pixelCount;
        }
        private Double GetMaximumDiameter(List<Point3d> points, int degree) {
            var diameter = 0.0;

            for (int i = 0; i < 180; i += degree) { // only 180 degrees because it doesnt matter which direction you're calculating the diameter from
                var xRotationMatrix = Calculate3dRotationMatrixXaxis(i / 180.0 * Math.PI);
                for (int j = 0; j <= 90; j += degree) { // only 90 degrees because while rotating in 3d on both axes you get the same diameter (see geogebra)
                    // checks every possible angle combination
                    var yRotationMatrix = Calculate3dRotationMatrixYaxis(j / 180.0 * Math.PI);
                    var maxZCoordinate = Double.NegativeInfinity;   
                    var minZCoordinate = Double.PositiveInfinity;
                    foreach (var point in points) {
                        // checks every possible point in the angle combinations
                        var transformedPoint = PointAndMatrixMultiplication(xRotationMatrix, point);
                        transformedPoint = PointAndMatrixMultiplication(yRotationMatrix, transformedPoint);
                        if (transformedPoint.Z < minZCoordinate) {
                            minZCoordinate = transformedPoint.Z;
                        }
                        if (transformedPoint.Z > maxZCoordinate) {
                            maxZCoordinate = transformedPoint.Z;
                        }
                    }
                    if (maxZCoordinate - minZCoordinate > diameter) {
                        diameter = maxZCoordinate - minZCoordinate;
                    }
                }
            }
            return diameter;
        }

        private Point3d PointAndMatrixMultiplication(Mat matrix, Point3d point) {
            return new Point3d(
                matrix.Get<Double>(0, 0) * point.X + matrix.Get<Double>(0, 1) * point.Y + matrix.Get<Double>(0, 2) * point.Z,
                matrix.Get<Double>(1, 0) * point.X + matrix.Get<Double>(1, 1) * point.Y + matrix.Get<Double>(1, 2) * point.Z,
                matrix.Get<Double>(2, 0) * point.X + matrix.Get<Double>(2, 1) * point.Y + matrix.Get<Double>(2, 2) * point.Z
                );
        }
        private Mat Calculate3dRotationMatrixXaxis(double radian) {
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

        private Mat Calculate3dRotationMatrixYaxis(double radian) {
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

        private List<Point3d> Get3dPointsForPore(List<LayerPore> layerPores, IDictionary<int, IBlobImage> blobImages, double ResX, double ResY, double ResZ) {
            List<Point3d> points = new List<Point3d>();
            foreach (var layerPore in layerPores) {
                Mat image = blobImages[layerPore.layerId].GetMask(layerPore.blobId);
                Cv2.FindContours(image, out contourPoints, out indexes, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                if (contourPoints.Length == 0) {
                    continue;
                }
                else {
                    foreach (var point in contourPoints[0]) {
                        points.Add(new Point3d(point.X * ResX, point.Y * ResY, layerPore.layerId * ResZ));    // transforms the pixel coordinates to real geometry
                    }
                }
            }
            return points;
        }
    }




    //NEMAZENYÉM//
    public class SimpleAlgorithm : PoreSizeAlgorithm {
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
        public Dictionary<int, SizeVolume> Calculate(
            Dictionary<int, List<LayerPore>> poresOnEachLayer,
            IDictionary<int, IBlobImage> blobImages,
            CancellationToken token,
            IProgress<double> progress,
            double ResX,
            double ResY,
            double ResZ) {
            var areaApproximation = ResX * ResY * ResZ;
            int done = 0;
            double max = poresOnEachLayer.Keys.Count();
            foreach (var onePore in poresOnEachLayer) {
                progress.Report(done / max);
                poreSizes.Add(onePore.Key, new SizeVolume() { Size = 0, Volume = 0 });

                if (token.IsCancellationRequested)
                    return poreSizes;

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
                BoundingBoxZ = onePore.Value.Select(x => x.layerId).Distinct().Count()*ResZ;
                poreSizes[onePore.Key].Size = GetMinimumSieveSize(BoundingBoxX, BoundingBoxY, BoundingBoxZ);
                poreSizes[onePore.Key].Volume = V;
                done++;
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
