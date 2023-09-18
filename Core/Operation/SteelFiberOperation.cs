using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using OpenCvSharp;
using System.Threading;
using Core.Operation.InternalOutputs;
using System.IO;
using System.Globalization;
using System.Text.Json;
using Core.Model.SteelFibers;
using Core.SteelFibers.Generation;
using Core.SteelFibers.SolutionChecker;
using Core.SteelFibers.Algorithm;

namespace Core.Operation
{
   public class SteelFiberOperation : IOperation
    {
        public string Name => "SteelFiberOperation";

        private readonly int blobMinSize = 10;  // a legkisebb méret (pixelszám), amitől egy blobot acélszálnak tekintünk, ez alatt csak zaj
        private readonly int elongatedBlobMinSize = 14;     // a legkisebb érték (px), aminél ha nagyobb egy irányban egy blob kiterjedése, akkor az hosszúkás
        private static int IdCounter = 0;

        Dictionary<int, Point> blobIdsWithSpeedVector;      // the last speedvector to each blob
        Dictionary<int, LayerAndVector> blobPositionsAndVectors;    // kulcs: SteelFiberId, Value: a blobok melyik layereken vanank, és hol
        List<SteelFiber> solution = new List<SteelFiber>();    // acélszálak ID-val és a hozzájuk tartozó blobokkal megadva

        Dictionary<int, List<Point>> blobPoints;
        Dictionary<int, bool> blobShapes;
        IAlgorithm algorithm = new HungarianAlgorithm();

        public OperationProperties DefaultOperationProperties => new SteelFiberOperationProperties();

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var NumberOfLayers = context.BlobImages.Count;
            for (var i = 0; i < NumberOfLayers; i++)
            {
                if (i == 0)
                {
                    SearchTheFirstBlobs(context);
                }
                else
                {
                    ConnectThisLayersBlobsToTheFormerLayers(i, context);
                }
                //lecsekkolni hogy a speedvectoros listaban a blobidk tenyleg rajta vannak ezen a koros layeren, es kivenni azokat amelyikek nincsenek
                checkSpeedvectorList(context.BlobImages.Values.ElementAt(i));
            }

            var json = JsonSerializer.Serialize(solution);
            File.WriteAllText(@"algoritmusMegoldas.json", json);

            return context;
        }

        private void checkSpeedvectorList(IBlobImage image)
        {
            var blobids = filterBlobIdsToSteelFiber(image);
            List<int> steelfiberids = new List<int>();
            foreach(int id in blobids)
            {
                steelfiberids.Add((int)image.GetTagValueOrNull(id, "SteelFiberId"));
            }
            foreach (int blobid in blobIdsWithSpeedVector.Keys)
            {
                if (!steelfiberids.Contains(blobid))
                {
                    blobIdsWithSpeedVector.Remove(blobid);
                }
            }
        }

        private Dictionary<int, Point > GetAllBlobCentersForLayer(IBlobImage blobImage, IEnumerable<int> blobIds)
        {
            Dictionary<int, Point> blobCenters = new Dictionary<int, Point>();
            for(int i = 0; i < blobIds.Count(); i++)
            {
                blobCenters.Add(blobIds.ElementAt(i), GetBlobCenter(blobIds.ElementAt(i), blobImage));
            }
            return blobCenters;
        }

        private bool ArePointsCloserThanDistance(Point first, Point second, double distance)
        {
            int x = first.X - second.X;
            int y = first.Y - second.Y;
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) <= distance ? true : false;
        }

        private List<int> filterBlobIdsToSteelFiber(IBlobImage image)
        {
            var blobIds = image.CollectAllRealBlobIds();
            var steelFiberBlobIds = new List<int>();
            foreach (int blobId in blobIds)
            {
                IEnumerable<ITag<int>> tags = image.GetTagsForBlob(blobId);
                foreach (ITag<int> tag in tags)
                {
                    if (tag.Name.Equals("STEEL_FIBER"))
                    {
                        steelFiberBlobIds.Add(blobId);
                    }
                }
            }
            return filterSmallBlobs(image,steelFiberBlobIds);
        }

        private List<int> filterSmallBlobs(IBlobImage image, List<int> blobIds)
        {
            Dictionary<int, int> blobSizes = new Dictionary<int, int>();
            foreach(int id in blobIds)
            {
                blobSizes.Add(id, 0);
            }
            for (int x = 0; x < image.Size.Width; x++)
            {
                for (int y = 0; y < image.Size.Height; y++)
                {
                    if (blobSizes.ContainsKey(image[x, y]))
                    {
                        blobSizes[image[x, y]]++;
                    }
                }
            }
            //foreach(int blob in blobSizes.Keys)
            for (int blob = blobSizes.Keys.Count - 1; blob >= 0; blob--)
            {
                if (blobSizes.TryGetValue(blob, out int j) && j < blobMinSize)
                {
                    blobSizes.Remove(blob);
                }
            }
            return blobSizes.Keys.ToList<int>();
        }

        private void SearchTheFirstBlobs(OperationContext context)
        {
            var blobImage = context.BlobImages.Values.ElementAt(0);
            List<int> blobIds = filterBlobIdsToSteelFiber(blobImage);
            lastLayer = blobImage;
            lastLayersBlobids = blobIds;

            blobPoints = new Dictionary<int, List<Point>>();
            for (int i = 0; i < blobIds.Count; i++)
            {
                blobPoints[i] = new List<Point>();
            }
            blobPoints = GetAllPointsForEveryBlob(blobImage, blobIds);
            blobShapes = GetBlobShapes(blobIds, blobPoints);

            blobIdsWithSpeedVector = new Dictionary<int, Point>();
            blobPositionsAndVectors = new Dictionary<int, LayerAndVector>();
            lastLayerBlobCenters = GetAllBlobCentersForLayer(lastLayer, lastLayersBlobids);
            foreach (int blobid in blobIds)
            {
                blobImage.SetTag(blobid, "SteelFiberId", IdCounter);
                blobIdsWithSpeedVector.Add(IdCounter, new Point(0, 0));
                blobPositionsAndVectors.Add(IdCounter, new LayerAndVector(new List<int>(), new List<Point>()));
                blobPositionsAndVectors[IdCounter].LayerNuber.Add(0);
                lastLayerBlobCenters.TryGetValue(blobid, out Point value);
                blobPositionsAndVectors[IdCounter].StartingPoint = value;
                var blobs = new Dictionary<int, int>();
                blobs.Add(0, blobid);
                solution.Add(new SteelFiber() { SteelFiberId = IdCounter, Blobs = blobs }); 
                IdCounter++;
            }
        }

        private Point GetBlobCenter(int id, IBlobImage blobImage)
        {
            int xsum = 0;
            int ysum = 0;
            int num = 0;
            for (int x = 0; x < blobImage.Size.Width; x++)
            {
                for (int y = 0; y < blobImage.Size.Height; y++)
                {
                    if (blobImage[x, y] == id)
                    {
                        num++;
                        xsum += x;
                        ysum += y;
                    }
                }
            }
            return new Point(xsum / num , ysum/num);
        }

        // segédváltozók, hogy egy layeren ne kelljen mindent kétszer kiszámolni
        private IBlobImage lastLayer;
        private IBlobImage thisLayer;
        private List<int> lastLayersBlobids;
        private List<int> thisLayersBlobids;
        private Dictionary<int, Point> lastLayerBlobCenters;
        private Dictionary<int, Point> thisLayerBlobCenters;
        private void ConnectThisLayersBlobsToTheFormerLayers(int frameNumber, OperationContext context)
        {
            // ha nem null (legalább egyszer már meghívódott ez a fgv.), akkor az előző futás aktuális értékei lesznek a mostani futás régi értékei
            // ha null, akkor most hívódott meg először, ekkor a SearchTheFirstBlobs-ban már megkapták az első layer-nek megfelelő értékeket
            if (thisLayer != null)  
            {
                lastLayer = thisLayer;
                lastLayersBlobids = thisLayersBlobids;
                lastLayerBlobCenters = thisLayerBlobCenters;
            }
            thisLayer = context.BlobImages.Values.ElementAt(frameNumber);
            thisLayersBlobids = filterBlobIdsToSteelFiber(thisLayer);
            thisLayerBlobCenters = GetAllBlobCentersForLayer(thisLayer, thisLayersBlobids);

            double[,] weights = CalculateWeightsBetweenBlobs(frameNumber, context, lastLayer, thisLayer, lastLayersBlobids, thisLayersBlobids, lastLayerBlobCenters, thisLayerBlobCenters); // [blob in the last layer, blob in this layer]
            
            Point[] assignmentsInWeightMatrix = algorithm.Solve(weights);

            List<int> blobsWithOutPair = new List<int>(thisLayersBlobids);

            for (int i = 0; i < assignmentsInWeightMatrix.Length; i++)
            {
                int idOfBlobInThisLayer = thisLayersBlobids.ElementAt(assignmentsInWeightMatrix[i].Y);
                int idOfBlobInLastLayer = lastLayersBlobids.ElementAt(assignmentsInWeightMatrix[i].X);

                if (weights[assignmentsInWeightMatrix[i].X, assignmentsInWeightMatrix[i].Y] <= 200)     // the blobs belong to the same fiber
                {
                    int blobId = (int)lastLayer.GetTagValueOrNull(idOfBlobInLastLayer, "SteelFiberId");
                    Point firstPoint = lastLayerBlobCenters[idOfBlobInLastLayer];
                    Point secondPoint = thisLayerBlobCenters[idOfBlobInThisLayer];

                    blobsWithOutPair.Remove(idOfBlobInThisLayer);
                    thisLayer.SetTag(idOfBlobInThisLayer, "SteelFiberId", blobId);

                    if (blobIdsWithSpeedVector.TryGetValue(blobId, out _))
                    {
                        blobIdsWithSpeedVector[blobId] = new Point(secondPoint.X - firstPoint.X, secondPoint.Y - firstPoint.Y);
                        blobPositionsAndVectors[blobId].VectorList.Add(new Point(secondPoint.X - firstPoint.X, secondPoint.Y - firstPoint.Y));
                        blobPositionsAndVectors[blobId].LayerNuber.Add(frameNumber);
                        var elem = solution.FirstOrDefault(s => s.SteelFiberId == blobId);
                        if (elem is not null)
                        {
                            elem.Blobs.Add(frameNumber, idOfBlobInThisLayer);
                        } else
                        {
                            var blobs = new Dictionary<int, int>();
                            blobs.Add(frameNumber, idOfBlobInThisLayer);
                            var output = new SteelFiber() { SteelFiberId = blobId, Blobs = blobs };
                            solution.Add(output);
                        }
                    }
                }
                else    
                {

                }
            }

            foreach (int blob in blobsWithOutPair)  // hozzaadjuk a speedvectroros listahoz aminek nem talaltunk part
            {
                thisLayer.SetTag(blob, "SteelFiberId", IdCounter);
                blobIdsWithSpeedVector.Add(IdCounter, new Point(0, 0));
                blobPositionsAndVectors.Add(IdCounter, new LayerAndVector(new List<int>(), new List<Point>()));
                blobPositionsAndVectors[IdCounter].LayerNuber.Add(frameNumber);
                thisLayerBlobCenters.TryGetValue(blob, out Point value);
                blobPositionsAndVectors[IdCounter].StartingPoint = value;

                var blobs = new Dictionary<int, int>();
                blobs.Add(frameNumber, blob);
                solution.Add(new SteelFiber() { SteelFiberId = IdCounter, Blobs = blobs });

                IdCounter++;
            }

        }

        private double[,] CalculateWeightsBetweenBlobs(int frameNumber, OperationContext context, IBlobImage lastLayer, IBlobImage thisLayer, List<int> lastLayersBlobids, List<int> thisLayersBlobids, Dictionary<int, Point> lastLayerBlobCenters, Dictionary<int, Point> thisLayerBlobCenters)
        {           
            double[,] weights = new double[lastLayersBlobids.Count, thisLayersBlobids.Count];

            Dictionary<int, List<Point>> newBlobPoints = GetAllPointsForEveryBlob(thisLayer, thisLayersBlobids);

            // true: shape is elongated
            Dictionary<int, bool> newBlobShapes = GetBlobShapes(thisLayersBlobids, newBlobPoints);

            for (int lastBlob = 0; lastBlob < lastLayersBlobids.Count; lastBlob++)
            {
                for (int thisBlob = 0; thisBlob < thisLayersBlobids.Count; thisBlob++)
                {
                    int lastBlobId = lastLayersBlobids.ElementAt(lastBlob);
                    int thisBlobId = thisLayersBlobids.ElementAt(thisBlob);
                    Point speedVector = new Point(0, 0);
                    if (blobIdsWithSpeedVector.ContainsKey(lastBlobId))
                    {
                        speedVector = blobIdsWithSpeedVector[lastBlobId];
                    }
                    double distance = GetDistanceBetweenBlobCenters(lastLayerBlobCenters[lastBlobId] + speedVector, thisLayerBlobCenters[thisBlobId]);
                    double overlap = GetOverlappingPointsInBothBlobs(lastLayer, lastBlobId, thisLayer, thisBlobId, newBlobPoints);
                    double weightFromShapes = GetSimilarityBetweenBlobShapes(blobShapes, newBlobShapes, lastBlobId, thisBlobId);
                    double elongatedBlobDirection = GetWeightFromElongatedBlobDirection(blobShapes, newBlobShapes, lastBlobId, thisBlobId, blobPoints, newBlobPoints, lastLayerBlobCenters, thisLayerBlobCenters);
                    double minDistance = GetMinimalDistanceBetweenBlobs(blobPoints[lastBlobId], newBlobPoints[thisBlobId]);
                    double distFromDirectionOfElongatedBlob = GetDistanceFromElongatedBlobsDirection(blobShapes, newBlobShapes, lastBlobId, thisBlobId, blobPoints, newBlobPoints, lastLayerBlobCenters, thisLayerBlobCenters);

                    double weight = distance + weightFromShapes + elongatedBlobDirection + minDistance + distFromDirectionOfElongatedBlob / 5.0 - (overlap * 3.0);
                    weights[lastBlob, thisBlob] = Math.Max(weight, 0);  // weights for the Hungarian algorothm could not be negative
                }
            }

            blobPoints = newBlobPoints;
            blobShapes = newBlobShapes;
            return weights;
        }

        private double GetDistanceFromElongatedBlobsDirection(Dictionary<int, bool> blobShapes, Dictionary<int, bool> newBlobShapes, int lastBlobId, int thisBlobId, Dictionary<int, List<Point>> blobPoints, Dictionary<int, List<Point>> newBlobPoints, Dictionary<int, Point> lastLayerBlobCenters, Dictionary<int, Point> thisLayerBlobCenters)
        {
            if (!blobShapes[lastBlobId])      // not elongated -> no direction
            {
                return 0.0;
            }

            Point center = lastLayerBlobCenters[lastBlobId];
            Point thisCenter = thisLayerBlobCenters[thisBlobId];
            Point direction = GetSlopeOfBlob(blobPoints[lastBlobId], center);

            if (direction.X == 0)
            {
                return Math.Abs(thisCenter.X - center.X);
            }
            if (direction.Y == 0)
            {
                return Math.Abs(thisCenter.Y - center.Y);
            }

            // Distance from a point to a line:
            // https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line#Line_defined_by_an_equation
            double a = direction.Y;
            double b = -direction.X; 
            double c = -(direction.Y * center.X - direction.X * center.Y);

            double distance = Math.Abs(a * thisCenter.X + b * thisCenter.Y + c) / Math.Sqrt(a * a + b * b);
            return distance;
        }

        private double GetMinimalDistanceBetweenBlobs(List<Point> firstPoints, List<Point> secondPoints)
        {
            double min = int.MaxValue;
            foreach (Point A in firstPoints)
            {
                foreach (Point B in secondPoints)
                {
                    int x = A.X - B.X;
                    int y = A.Y - B.Y;
                    double distance = Math.Sqrt(x * x + y * y);
                    if (distance < min)
                    {
                        min = distance;
                    }
                }
            }
            return min;
        }

        // the weight, determined by the directions the two elongated blobs are facing
        private double GetWeightFromElongatedBlobDirection(Dictionary<int, bool> blobShapes, Dictionary<int, bool> newBlobShapes, int lastBlobId, int thisBlobId, Dictionary<int, List<Point>> blobPoints, Dictionary<int, List<Point>> newBlobPoints, Dictionary<int, Point> lastLayerBlobCenters, Dictionary<int, Point> thisLayerBlobCenters)
        {
            if (!blobShapes[lastBlobId] || !newBlobShapes[thisBlobId])      // at least one of them is not elongated -> no weight from similar directions
            {
                return 0.0;
            }
            Point lastVector = GetSlopeOfBlob(blobPoints[lastBlobId], lastLayerBlobCenters[lastBlobId]);
            Point thisVector = GetSlopeOfBlob(newBlobPoints[thisBlobId], thisLayerBlobCenters[thisBlobId]);
            double lastSlope;
            double thisSlope;

            if (lastVector.X == 0)
            {
                if (thisVector.X == 0)
                {
                    return 0.0;
                }
                thisSlope = Math.Abs(thisVector.Y / thisVector.X);
                return thisSlope * 3.0;
            } 
            else if (thisVector.X == 0)
            {
                lastSlope = Math.Abs(lastVector.Y / lastVector.X);
                return lastSlope * 3.0;
            }

            lastSlope = lastVector.Y / lastVector.X;
            thisSlope = thisVector.Y / thisVector.X;
            return Math.Abs(lastSlope - thisSlope) * 3.0;
        }

        // subtracts the farthest point of the blob from the center -> accurate estimate for the slope of the blob
        private Point GetSlopeOfBlob(List<Point> points, Point center)
        {
            double max = -1.0;
            Point farthestPointOfBlob = new Point();
            foreach (Point p in points)
            {
                int x = center.X - p.X;
                int y = center.Y - p.Y;
                double distance = Math.Sqrt(x * x + y * y);
                if (distance > max)
                {
                    farthestPointOfBlob = p;
                    max = distance;
                }
            }
            return farthestPointOfBlob - center;
        }

        private double GetSimilarityBetweenBlobShapes(Dictionary<int, bool> blobShapes, Dictionary<int, bool> newBlobShapes, int lastBlob, int thisBlob)
        {
            bool lastShape = blobShapes[lastBlob];
            bool thisShape = newBlobShapes[thisBlob];
            double weight = 0.0;
            if (lastShape == thisShape)     // they have a similar shape
            {
                weight = 0.0;
            } else
            {
                weight = 25.0;
            }
            return weight;
        }

        private Dictionary<int, List<Point>> GetAllPointsForEveryBlob(IBlobImage thisLayer, List<int> thisLayersBlobids)
        {
            Dictionary<int, List<Point>> newBlobPoints = new Dictionary<int, List<Point>>();
            foreach (int id in thisLayersBlobids)
            {
                newBlobPoints.Add(id, new List<Point>());
            }

            for (int x = 0; x < thisLayer.Size.Width; x++)
            {
                for (int y = 0; y < thisLayer.Size.Height; y++)
                {
                    if (thisLayersBlobids.Contains(thisLayer[x, y]))
                    {
                        newBlobPoints[thisLayer[x, y]].Add(new Point(x, y));
                    }
                }
            }

            return newBlobPoints;
        }

        private Dictionary<int, bool> GetBlobShapes(List<int> thisLayersBlobids, Dictionary<int, List<Point>> newBlobPoints)
        {
            bool isThisBlobElongated;

            Dictionary<int, bool> blobShapes = new Dictionary<int, bool>();

            foreach (int id in thisLayersBlobids)
            {
                int minX = int.MaxValue;
                int minY = int.MaxValue;
                int maxX = int.MinValue;
                int maxY = int.MinValue;
                foreach (Point p in newBlobPoints[id])
                {
                    if (p.X < minX)
                    {
                        minX = p.X;
                    } else if (p.X > maxX)
                    {
                        maxX = p.X;
                    }
                    if (p.Y < minY)
                    {
                        minY = p.Y;
                    }
                    else if (p.Y > maxY)
                    {
                        maxY = p.Y;
                    }
                }
                bool smallX = maxX - minX < elongatedBlobMinSize;
                bool smallY = maxY - minY < elongatedBlobMinSize;
                if (smallX && smallY)   // small in every direction, not elongated
                {
                    isThisBlobElongated = false;
                } else   // long at least in one direction, elongated
                {
                    isThisBlobElongated = true;
                }
                Point lengthVector = new Point(maxX, maxY) - new Point(minX, minY);
                if (Math.Sqrt(lengthVector.X * lengthVector.X + lengthVector.Y * lengthVector.Y) >= elongatedBlobMinSize)
                {
                    isThisBlobElongated = true;     // blob lays diagonally, and is long
                }
                blobShapes.Add(id, isThisBlobElongated);
            }
            return blobShapes;
        }

        private double GetOverlappingPointsInBothBlobs(IBlobImage lastLayer, int lastBlob, IBlobImage thisLayer, int thisBlob, Dictionary<int, List<Point>> newBlobPoints)
        {
            int counter = 0;
            foreach (Point p in newBlobPoints[thisBlob])
            {
                if (blobPoints[lastBlob].Contains(p))
                {
                    counter++;
                }
            }
            return counter;
        }

        private double GetDistanceBetweenBlobCenters(Point first, Point second)
        {
            int x = first.X - second.X;
            int y = first.Y - second.Y;
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
        }

        #region Visualization
        private async void WriteAllResultsMatlab(double xResolution, double yResolution, double zResolution)
        {
            string outp = "figure;hold on;\ngrid on; \nxlabel('X axis (mm)'), ylabel('Y axis (mm)'), zlabel('Z axis (mm)'); \nset(gca, 'CameraPosition',[1 2 3]); \n";
            outp += $"origin = [0,0,0];\n";
            foreach (var item in blobPositionsAndVectors)
            {
                Point allMovement = new Point(0, 0);
                foreach (var vec in item.Value.VectorList)
                {
                    allMovement += vec;
                }
                Point3d movement3D = new Point3d();
                movement3D.X = allMovement.X * xResolution + item.Value.StartingPoint.X * xResolution;
                movement3D.Y = allMovement.Y * yResolution + item.Value.StartingPoint.X * xResolution;
                movement3D.Z = (item.Value.LayerNuber.Count - 1) * zResolution + item.Value.LayerNuber[0] * zResolution;

                outp += $"plot3([{(item.Value.StartingPoint.X * xResolution).ToString("G", CultureInfo.InvariantCulture)} " + movement3D.X.ToString("G", CultureInfo.InvariantCulture) +
                    $"],[{(item.Value.StartingPoint.Y * yResolution).ToString("G", CultureInfo.InvariantCulture)} " + movement3D.Y.ToString("G", CultureInfo.InvariantCulture) +
                    $"],[{(item.Value.LayerNuber[0] * zResolution).ToString("G", CultureInfo.InvariantCulture)} " + movement3D.Z.ToString("G", CultureInfo.InvariantCulture) +
                    "],'b-^', 'LineWidth',1);\n";
            }

            await File.WriteAllTextAsync(@"OrientationsOfSteelFibers.m", outp);
        }

        private async void WriteDistribution(int[] counter)
        {
            string outp = "";

            for (int i = 0; i < 36; i++)
            {
                outp += counter[i] + "\n";
            }
            await File.WriteAllTextAsync("distributionByDirection.txt", outp);
        }
        // for visualization
        private int[] CalculateDistribution()
        {
            int[] counter = new int[36];
            foreach (var item in blobPositionsAndVectors)
            {
                Point allMovement = new Point();
                double angle;

                foreach (var vector in item.Value.VectorList)
                {
                    allMovement += vector;
                }

                if (allMovement.X == 0 && allMovement.Y == 0)
                {
                    continue;
                }
                else if (allMovement.X == 0)
                {
                    angle = 0;
                }
                else
                {
                    angle = Math.Atan(allMovement.Y / allMovement.X);
                    if (allMovement.X > 0 && allMovement.Y > 0)
                    {
                        angle = Math.PI / 2 - angle;
                    }
                    else if (allMovement.X > 0 && allMovement.Y < 0)
                    {
                        angle = Math.PI / 2 - angle;
                    }
                    else if (allMovement.X < 0 && allMovement.Y < 0)
                    {
                        angle = Math.PI * 1.5 - angle;
                    }
                    else if (allMovement.X < 0 && allMovement.Y < 0)
                    {
                        angle = Math.PI * 1.5 - angle;
                    }
                }
                for (int i = 0; i < 36; i++)
                {
                    if (angle > i * Math.PI / 18.0 && angle <= (i + 1) * Math.PI / 18.0)
                    {
                        counter[i]++;
                    }
                }
            }
            return counter;
        }

        private double GetMeanLength(double xResolution, double yResolution, double zResolution)
        {
            double mean = 0.0;
            foreach (var steelFiber in blobPositionsAndVectors)
            {
                Point sum = new(0,0);
                foreach (var vector in steelFiber.Value.VectorList)
                {
                    sum += vector;
                }
                mean += Math.Sqrt(sum.X * xResolution * sum.X * xResolution + sum.Y * yResolution * sum.Y * yResolution +
                    steelFiber.Value.VectorList.Count * zResolution * steelFiber.Value.VectorList.Count * zResolution);
            }
            return mean / blobPositionsAndVectors.Count;
        }
        #endregion Visualization

    }

    internal class LayerAndVector
    {
        public List<int> LayerNuber { get; set; }
        public List<Point> VectorList { get; set; }
        public Point StartingPoint { get; set; } = new();
        public LayerAndVector(List<int> layerNuber, List<Point> vectorList)
        {
            LayerNuber = layerNuber;
            VectorList = vectorList;
        }
    }

    public class SteelFiberOperationProperties : OperationProperties
    {

    }
}