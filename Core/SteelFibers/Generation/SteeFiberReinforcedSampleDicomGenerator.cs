using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using System.Drawing;
using Dicom.IO.Buffer;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;
using System.Text.Json;
using System.IO;
using Core.Model.SteelFibers;
using Core.SteelFibers.Converters;
using Newtonsoft.Json;

namespace Core.SteelFibers.Generation
{
    public class SteeFiberReinforcedSampleDicomGenerator
    {
        private const double dividerKonstant = 4.0;
        public int imageWidth = 400;
        public int imageHeight = 400;
        public readonly double mmsPerPixel = 0.390625;
        public readonly double mmsBetweenImages = 1.2;
        public readonly string configFile;
        public readonly string outputDicomFile;
        public readonly string outputSolutionFile;
        public double diameter = 1.3;
        public int SteelFiberCount = 20;
        public int touchingFibersCount = 0;
        public double oneDirection = 0;
        public double thetaMean = 60;
        public double phiMean = 45;
        public double lengthMean = 30;
        public double lengthMin = 26;
        public double lengthMax = 34;
        public double straightness = 2;
        public int imageCount = 20;
        public int randSeed = 1;


        /// <param name="configFile">a felhasználói paraméterek fájlja (ha van ilyen), kiterjesztéssel</param>
        /// <param name="outputDicomFile"> kiterjesztés nélkül, generált kimeneti fájlok neve (és elérési útja)</param>
        /// <param name="outputSolutionFile">az elvért kimenet json fájlja (kiterjesztés néklkül) </param>
        public SteeFiberReinforcedSampleDicomGenerator(string configFile, string outputDicomFile, string outputSolutionFile)
        {
            if (!string.IsNullOrEmpty(configFile))
            {
                this.configFile = configFile;
            }
            if (string.IsNullOrEmpty(outputDicomFile))
            {
                throw new ArgumentException($"'{nameof(outputDicomFile)}' cannot be null or empty", nameof(outputDicomFile));
            }

            if (string.IsNullOrEmpty(outputSolutionFile))
            {
                throw new ArgumentException($"'{nameof(outputSolutionFile)}' cannot be null or empty", nameof(outputSolutionFile));
            }

            
            this.outputDicomFile = outputDicomFile;
            this.outputSolutionFile = outputSolutionFile;
        }

        public void GenerateInputFiles()
        {
            string[] lines = File.ReadAllLines(configFile);

            foreach (var line in lines)
            {
                var temp = line.Split(":");
                switch (temp[0])
                {
                    case "Acélszálak összesen":
                        SteelFiberCount = int.Parse(temp[1]);
                        break;
                    case "Összeérő szálak száma":
                        touchingFibersCount = int.Parse(temp[1]);
                        break;
                    case "Egyirányúság":
                        oneDirection = double.Parse(temp[1], System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    case "Irány átlag":
                        var temp2 = temp[1].Split("-");
                        thetaMean = double.Parse(temp2[0]);
                        phiMean = double.Parse(temp2[1]);
                        break;
                    case "Hossz átlag":
                        lengthMean = double.Parse(temp[1]);
                        break;
                    case "Hossz min":
                        lengthMin = double.Parse(temp[1]);
                        break;
                    case "Hossz max":
                        lengthMax = double.Parse(temp[1]);
                        break;
                    case "Egyenesség":
                        straightness = double.Parse(temp[1]);
                        break;
                    case "Képek száma":
                        imageCount = int.Parse(temp[1]);
                        break;
                    case "Random seed":
                        randSeed = int.Parse(temp[1]);
                        break;
                    case "Kép szélessége":
                        imageWidth = int.Parse(temp[1]);
                        break;
                    case "Kép magassága":
                        imageHeight = int.Parse(temp[1]);
                        break;
                    case "Acélszálak átmérője":
                        diameter = double.Parse(temp[1]);
                        break;
                    default:
                        throw new ArgumentException("A bemeneti fájl nem emgfelelő nevű paramétert tartalmaz.");
                }
            }
            var steelFibers = GenerateImagesWithSolution();

            PngToDicomConverter converter = new PngToDicomConverter();
            converter.ConvertImages(outputDicomFile, ".png", outputDicomFile, imageCount);

            // a végeselem szimuláció számára
            List<List<Point3d>> segments = new();
            foreach (var item in steelFibers)
            {
                List<Point3d> segmentInMm = new();
                foreach (var element in item.Segments)
                {
                    segmentInMm.Add(new Point3d(element.X * mmsPerPixel, element.Y * mmsPerPixel, element.Z * mmsBetweenImages));
                }
                segments.Add(segmentInMm);
            }
            string json = string.Empty;
            json = JsonConvert.SerializeObject(segments);
            File.WriteAllText(outputDicomFile + ".json", json);
        }

        private List<SteelFiberLine> GenerateImagesWithSolution()
        {
            Random rand = new Random(randSeed);
            List<Mat> imageList = new();
            List<Blob> blobs = new();
            for (int i = 0; i < imageCount; i++)
            {
                var image = new Mat(imageHeight, imageWidth, MatType.CV_8UC3);
                Cv2.Rectangle(image, new OpenCvSharp.Point(0, 0), new OpenCvSharp.Point(imageWidth, imageHeight), new Scalar(0, 0, 0), -1);
                imageList.Add(image);
            }
            
            List<SteelFiberLine> steelFibers = new();
            List<SteelFiberLine> everySteelFiber = new();
            var centers = new List<Point3i>();

            List<SteelFiberLine> touchingSteelFibers = new();
            while (touchingSteelFibers.Count < touchingFibersCount)
            {
                SteelFiberLine firstFiber, secondFiber;
                (firstFiber, secondFiber) = GenerateTouchingSteelFibersPair(rand);
                
                var firstCentersNew = new List<Point3i>();
                firstCentersNew = GetNewCenters(imageList, firstFiber);
                var secondCentersNew = new List<Point3i>();
                secondCentersNew = GetNewCenters(imageList, secondFiber);
                var commonPoint = firstFiber.Segments.First(p => secondFiber.Segments.Contains(p));

                if (commonPoint.Z >= 0 && commonPoint.Z < imageCount && !CloserToOtherFibersThanDist(centers, firstCentersNew, 20) && 
                    !CloserToOtherFibersThanDist(centers, secondCentersNew, 20) && 
                    !CloserToOtherFibersThanDist(firstCentersNew, secondCentersNew, 20))
                {
                    List<Point3i> kozeppontok = new();
                    List<int> hosszak = new();
                    var oldImageList = new List<Mat>();
                    foreach (var image in imageList)
                    {
                        oldImageList.Add(image.Clone());
                    }
                    (imageList, kozeppontok, hosszak) = DrawSteelFiber(imageList, firstFiber);
                    centers.AddRange(kozeppontok);
                    touchingSteelFibers.Add(firstFiber);
                    var newBlobsFirst = GetBlobPoints(imageList, oldImageList, kozeppontok, hosszak, touchingSteelFibers.Count - 1);
                    blobs.AddRange(newBlobsFirst);

                    oldImageList = new List<Mat>();
                    foreach (var image in imageList)
                    {
                        oldImageList.Add(image.Clone());
                    }
                    (imageList, kozeppontok, hosszak) = DrawSteelFiber(imageList, secondFiber);
                    var newBlobs = GetBlobPoints(imageList, oldImageList, kozeppontok, hosszak, touchingSteelFibers.Count - 1);
                    if (!newBlobs.Any(b => b.Pixels.Count == 0))
                    {
                        centers.AddRange(kozeppontok);
                        touchingSteelFibers.Add(secondFiber);
                        newBlobs.Remove(newBlobs.First(b => b.Pixels.Any(p => p.Z == commonPoint.Z)));   // kivesszük a közös blobot, ami az érintkezésüknél van
                        blobs.AddRange(newBlobs);
                    }
                }
            }
            everySteelFiber.AddRange(touchingSteelFibers);

            while (steelFibers.Count < SteelFiberCount)
            {
                SteelFiberLine fiber = GenerateNormalSteelFiber(rand);
                var centersNew = new List<Point3i>();
                centersNew = GetNewCenters(imageList, fiber);

                if (!CloserToOtherFibersThanDist(centers, centersNew, 40))
                {
                    List<Point3i> kozeppontok = new();
                    List<int> hosszak = new();
                    var oldImageList = new List<Mat>();
                    foreach (var image in imageList)
                    {
                        oldImageList.Add(image.Clone());
                    }
                    (imageList, kozeppontok, hosszak) = DrawSteelFiber(imageList, fiber);
                    var newBlobs = GetBlobPoints(imageList, oldImageList, kozeppontok, hosszak, steelFibers.Count);
                    if (!newBlobs.Any(b => b.Pixels.Count == 0))
                    {
                        centers.AddRange(kozeppontok);
                        steelFibers.Add(fiber);
                        blobs.AddRange(newBlobs);
                    }
                }
            }
            everySteelFiber.AddRange(steelFibers);

            List<SteelFiber> solution = CreateSolution(imageList, blobs, everySteelFiber);
            var json = System.Text.Json.JsonSerializer.Serialize(solution);

            File.WriteAllText(outputSolutionFile + ".json", json);

            for (int i = 0; i < imageList.Count; i++)
            {
                OpenCvSharp.Mat image = imageList[i];
                Cv2.ImWrite(outputDicomFile + i + ".png", image);
            }

            return everySteelFiber;
        }

        public List<Point3i> GetNewCenters(List<Mat> imageList, SteelFiberLine fiber)
        {
            var centersNew = new List<Point3i>();
            for (int i = 0; i < imageList.Count; i++)
            {
                for (int j = 0; j < fiber.Segments.Count - 1; j++)
                {
                    Point3i point1 = fiber.Segments[j];
                    Point3i point2 = fiber.Segments[j + 1];
                    if (point1.Z <= i * mmsBetweenImages && point2.Z >= i * mmsBetweenImages)
                    {
                        var arany = (i * mmsBetweenImages - point1.Z) / ((point2.Z - point1.Z) == 0 ? 0 : (point2.Z - point1.Z));
                        var xCoord = point1.X + (point2.X - point1.X) * arany;
                        var yCoord = point1.Y + (point2.Y - point1.Y) * arany;
                        var center = new OpenCvSharp.Point(xCoord, yCoord);
                        centersNew.Add(new(center.X, center.Y, i));
                    }
                }
            }
            return centersNew;
        }

        public List<SteelFiber> CreateSolution(List<Mat> imageList, List<Blob> blobs, List<SteelFiberLine> steelFibers)
        {
            // blobok id-jának meghatározása:
            for (int i = 0; i < imageList.Count; i++)
            {
                int idCounter = 2;  // az első, nem háttér blob a 2-es id-jű
                Mat image = imageList[i];
                var indexer = image.GetGenericIndexer<Vec3b>();
                // ilyen sorrendben kell végiglépnünk a képpontokon
                for (int xCoord = 0; xCoord < imageWidth; xCoord++)
                {
                    for (int yCoord = 0; yCoord < imageHeight; yCoord++)
                    {
                        if (indexer[yCoord, xCoord] != new Vec3b(0, 0, 0))
                        {
                            var blob = blobs.SingleOrDefault(b => b.Pixels.Contains(new Point3i(xCoord, yCoord, i)));
                            if (blob == null)   // fehér pont, de nem tartozik blobhoz -> zaj
                            {
                                // TODO : ilyenek még nincsenek generálva, ha lesznek, akkor ezeket is blobként kéne tárolni, de nem rendelni hozzá acélszálat
                            }
                            else if (blob.Id == 0)  // fehér pont, blobhoz tartozik, és ennek a blobnak még nem adtunk id-t
                            {
                                blob.Id = idCounter;
                                idCounter++;
                            }
                        }
                    }
                }
            }
            List<int> idList = GetFiberIds(blobs, steelFibers);

            List<SteelFiber> solution = new List<SteelFiber>();
            for (int i = 0; i < steelFibers.Count; i++)
            {
                var steelFiber = new SteelFiber();
                steelFiber.SteelFiberId = i;
                SortedDictionary<int, int> blobDicitonary = new();

                SteelFiberLine touchingSteelFiber = steelFibers.FirstOrDefault(sf => sf.TouchingFiber > -1 && sf.TouchingFiber == steelFibers[i].TouchingFiber && sf != steelFibers[i]);
                if (touchingSteelFiber is not null)
                {
                    int pos = steelFibers.IndexOf(touchingSteelFiber);
                    List<Blob> touchingFibersBlobs = blobs.Where(b => b.SteelFiberID == pos).ToList();
                    List<Blob> steelFibersBlobs = blobs.Where(b => b.SteelFiberID == i).ToList();
                    var intersectionZCoord = touchingSteelFiber.Segments.First(p => touchingFibersBlobs.Any(b => b.Pixels.Contains(p))).Z;
                    int intersectionZCoord3 = -1;
                    for (int j = 0; j < touchingFibersBlobs.Count; j++)
                    {
                        if (!steelFibersBlobs.Any(b => b.Pixels[0].Z == touchingFibersBlobs[i].Pixels[0].Z))
                        {
                            intersectionZCoord3 = touchingFibersBlobs[i].Pixels[0].Z;
                        }
                    }
                    var intersectionZCoord2 = touchingFibersBlobs.First(p => steelFibersBlobs.Any(b => !b.Pixels.Any(pixel => pixel.Z == p.Pixels[0].Z))).Pixels[0].Z;
                    foreach (var blob in blobs)
                    {
                        if (!blobDicitonary.ContainsKey(blob.Pixels[0].Z) && (blob.SteelFiberID == idList[i] ||
                            blob.SteelFiberID == idList.IndexOf(pos) && blob.Pixels[0].Z == intersectionZCoord3))
                        {
                            blobDicitonary.Add(blob.Pixels[0].Z, blob.Id);
                        }
                    }
                }
                else
                {
                    foreach (var blob in blobs)
                    {
                        if (blob.SteelFiberID == idList[i] && !blobDicitonary.ContainsKey(blob.Pixels[0].Z))
                        {
                            blobDicitonary.Add(blob.Pixels[0].Z, blob.Id);
                        }
                    }
                }
                steelFiber.Blobs = new Dictionary<int, int>(blobDicitonary);
                solution.Add(steelFiber);
            }

            return solution;
        }

        public List<int> GetFiberIds(List<Blob> blobs, List<SteelFiberLine> steelFibers)
        {
            List<int> idList = new List<int>();
            List<int> minimums = new List<int>();
            for (int i = 0; i < steelFibers.Count; i++)
            {
                idList.Add(i);
                int firstLayer = 100000;
                for (int j = 0; j < blobs.Count; j++)
                {
                    Blob blob = blobs[j];
                    if (blob.SteelFiberID == i)
                    {
                        if (firstLayer > blob.Pixels[0].Z)
                        {
                            firstLayer = blob.Pixels[0].Z;
                        }
                    }
                }
                minimums.Add(firstLayer);
            }
            for (int i = 0; i < steelFibers.Count - 1; i++)
            {
                for (int j = i + 1; j < steelFibers.Count; j++)
                {
                    if (minimums[idList[i]] > minimums[idList[j]])
                    {
                        var temp = idList[i];
                        idList[i] = idList[j];
                        idList[j] = temp;
                    }
                }
            }
            return idList;
        }

        private List<Blob> GetBlobPoints(List<Mat> imageList, List<Mat> oldImageList, List<Point3i> kozeppontok, List<int> hosszak, int steelFiberIndex)
        {
            List<Blob> blobs = new();
            Vec3b Black = new Vec3b(0, 0, 0);
            for (int i = 0; i < kozeppontok.Count; i++)
            {
                var vizsgaltBlob = new Blob();
                vizsgaltBlob.SteelFiberID = steelFiberIndex;
                vizsgaltBlob.Pixels = new();

                var minX = kozeppontok[i].X - hosszak[i] - 2;
                var maxX = kozeppontok[i].X + hosszak[i] + 2;
                var minY = kozeppontok[i].Y - hosszak[i] - 2;
                var maxY = kozeppontok[i].Y + hosszak[i] + 2;
                var oldIndexer = oldImageList[kozeppontok[i].Z].GetGenericIndexer<Vec3b>();
                var newIndexer = imageList[kozeppontok[i].Z].GetGenericIndexer<Vec3b>();
                for (int xCoord = minX < 0 ? 0 : minX; xCoord < (maxX > imageHeight ? imageHeight : maxX ); xCoord++)
                {
                    for (int yCoord = minY < 0 ? 0 : minY; yCoord < (maxY > imageWidth ? imageWidth : maxY); yCoord++)
                    {
                        // korábban nem volt itt pont, most már lett a rajzolás után -> a blobhoz tartozik:
                        if (oldIndexer[yCoord, xCoord] == Black && newIndexer[yCoord, xCoord] != Black)
                        {
                            vizsgaltBlob.Pixels.Add(new Point3i(xCoord, yCoord, kozeppontok[i].Z));
                        }
                    }
                }
                blobs.Add(vizsgaltBlob);
            }
            return blobs;
        }

        private (List<Mat>, List<Point3i>, List<int>) DrawSteelFiber(List<Mat> imageList, SteelFiberLine fiber)
        {
            var centers = new List<Point3i>();
            var lengths = new List<int>();
            for (int i = 0; i < imageList.Count; i++)
            {
                Mat image = imageList[i];

                for (int j = 0; j < fiber.Segments.Count - 1; j++)
                {
                    Point3i point1 = fiber.Segments[j];
                    Point3i point2 = fiber.Segments[j + 1]; // két szoomszédos töréspont
                    if (point1.Z <= i && point2.Z >= i)
                    {
                        double magassag = (point2.Z - point1.Z);
                        double arany = 0;
                        if (magassag != 0)
                        {
                            arany = (i - point1.Z) / magassag;
                        }
                        var xCoord = point1.X + (point2.X - point1.X) * arany;
                        var yCoord = point1.Y + (point2.Y - point1.Y) * arany;

                        var center = new OpenCvSharp.Point(xCoord, yCoord);

                        var dZ = (point2.Z - point1.Z) / mmsPerPixel;
                        var dXY = Math.Sqrt((point2.X - point1.X) * (point2.X - point1.X) + (point2.Y - point1.Y) * (point2.Y - point1.Y));
                        double dolesszog;
                        if (dXY == 0)
                        {
                            dolesszog = Math.PI / 2.0;
                        } else
                        {
                            dolesszog = Math.PI / 2.0 - Math.Atan( dZ/ dXY);
                        }

                        var max = Math.Max(diameter / mmsPerPixel / Math.Cos(dolesszog), diameter / mmsPerPixel);
                        var min = Math.Min(max, diameter / mmsPerPixel * 10.0);

                        var axes = new OpenCvSharp.Size(min, diameter / mmsPerPixel);  

                        double angle;
                        if ((point1.X - point2.X) == 0)
                        {
                            angle = 0;
                        }
                        else
                        {
                            angle = Math.Atan((double)(point1.Y - point2.Y) / (double)(point1.X - point2.X)) / Math.PI * 180;
                        }
                        Scalar color = new Scalar(255, 255, 255);
                        Cv2.Ellipse(image, center, axes, angle, 0, 360, color, -1);
                        centers.Add(new (center.X, center.Y, i));
                        lengths.Add(axes.Height > axes.Width ? axes.Height : axes.Width);
                        break;  // ha egy képre egyszer már rajzoltunk egy blobot, akkor többet nem kell
                    }
                }
            }
            return (imageList, centers, lengths);
        }

        public bool CloserToOtherFibersThanDist(List<Point3i> centers, List<Point3i> centersNew, int dist) 
        {
            // van-e olyan eleme az új középpontoknak, amely közelebb van a már kirajzol középpontokhoz, mint a megadott távolság?
            return centersNew.Any(cNew => centers.Any(c => c.Z == cNew.Z && Math.Pow(c.X - cNew.X, 2) + Math.Pow(c.Y - cNew.Y, 2) < dist * dist));
        }

        // normál, szakaszokból álló acélszál, nem metsz másikat
        private SteelFiberLine GenerateNormalSteelFiber(Random rand)
        {
            var fiber = new SteelFiberLine();

            var pxLength = RandomLengthDistribution(rand);
            var realLength = pxLength * mmsPerPixel;    // hossz mm-ben

            var theta = RandomDistribution(rand, thetaMean / 180.0 * Math.PI, rand.NextDouble() * (Math.PI * oneDirection) / 2.0);    // szög az áltag körül random gauss eloszlás szerint, maximum adott eltéréssel
            var phi = RandomDistribution(rand, phiMean / 180.0 * Math.PI, rand.NextDouble() * (Math.PI * oneDirection));

            // lehet, hogy a megfelelő intervallumon kívülre esnek, emiatt az adott intervallumba forgatjuk őket
            theta = NormalizeAngle(theta, Math.PI);
            phi = NormalizeAngle(phi, 2.0 * Math.PI);

            // nem baj, ha kilógnak, előfordul a valós mintákkal is, hogy az acélszál el van vágva a betonkocka kivágásakor
            fiber.StartPoint = new Point3i(rand.Next(0, imageWidth),
                                           rand.Next(0, imageHeight),
                                           rand.Next(0, (int)((imageCount - 1))));

            fiber.EndPoint = new Point3i(fiber.StartPoint.X + (int)(pxLength * Math.Sin(theta) * Math.Cos(phi)),
                                         fiber.StartPoint.Y + (int)(pxLength * Math.Sin(theta) * Math.Sin(phi)),
                                         fiber.StartPoint.Z + (int)(pxLength * Math.Cos(theta) / mmsBetweenImages / dividerKonstant));  //  osztás -> mert túl laposan állnak a szálak enélkül

            fiber.Segments.Add(fiber.StartPoint);

            var magassag = fiber.EndPoint.Z - fiber.StartPoint.Z;
            for (int i = 1; i <= straightness; i++)
            {
                var alap = (int)(magassag / (straightness + 1) * i);
                var veletlen = rand.Next(-1, 1);
                var segmentPoint = new Point3i(
                    fiber.Segments[i - 1].X + (int)(pxLength / (straightness + 1) * i * Math.Sin(theta) * Math.Cos(phi)) + rand.Next(-(int)pxLength / 20, (int)pxLength / 20),
                    fiber.Segments[i - 1].Y + (int)(pxLength / (straightness + 1) * i * Math.Sin(theta) * Math.Sin(phi)) + rand.Next(-(int)pxLength / 20, (int)pxLength / 20),
                    fiber.StartPoint.Z + alap + veletlen
                    );
                fiber.Segments.Add(segmentPoint);
            }
            fiber.Segments.Add(fiber.EndPoint);

            List<Point3i> deleteThese = new List<Point3i>();
            List<int> zCoords = new List<int>();
            for (int i = 0; i < fiber.Segments.Count; i++)
            {
                if (!zCoords.Contains(fiber.Segments[i].Z))
                {
                    zCoords.Add(fiber.Segments[i].Z);
                } else
                {
                    deleteThese.Add(fiber.Segments[i]);
                }
            }
            foreach (var item in deleteThese)
            {
                fiber.Segments.Remove(item);
            }
            fiber.Segments = fiber.Segments.OrderBy(p => p.Z).ToList();


            fiber.StartPoint = fiber.Segments[0];
            fiber.EndPoint = fiber.Segments.Last();

            return fiber;
        }

        public (SteelFiberLine, SteelFiberLine) GenerateTouchingSteelFibersPair(Random rand)
        {
            var firstFiber = GenerateNormalSteelFiber(rand);
            var touchPoint = rand.Next(firstFiber.Segments.Count);
            bool starting = rand.NextDouble() > 0.5;
            var secondFiber = new SteelFiberLine();
            Point3i pivotPoint = firstFiber.Segments[touchPoint];
            for (int i = 0; i < firstFiber.Segments.Count; i++)
            {
                if (i == touchPoint)
                {
                    secondFiber.Segments.Add(firstFiber.Segments[i]);
                } else
                {
                    Point3i segmentPoint = firstFiber.Segments[i];
                    int dX = segmentPoint.X - pivotPoint.X;
                    int dY = segmentPoint.Y - pivotPoint.Y;
                    int x = dX > 0 ? 1 : -1;
                    int y = dY > 0 ? 1 : -1;
                    Point3i mirroredPoint = new Point3i();
                    mirroredPoint.Z = segmentPoint.Z;
                    mirroredPoint.X = pivotPoint.X - x * Math.Abs(segmentPoint.Z - touchPoint) * 5;
                    mirroredPoint.Y = pivotPoint.Y - y * Math.Abs(segmentPoint.Z - touchPoint) * 5;
                    secondFiber.Segments.Add(mirroredPoint);    // elforgatott és távolított
                }
            }
            secondFiber.StartPoint = secondFiber.Segments[0];
            secondFiber.EndPoint = secondFiber.Segments.Last();
            return (firstFiber, secondFiber);
        }


        public double NormalizeAngle(double angle, double max)     // ha a max értéken (pl PI) kívül vagy 0 alatt van, akkor a 2 közé rakjuk
        {
            if (angle < 0)
            {
                return angle + max;
            }
            else if (angle > max)
            {
                return angle - max;
            }
            return angle;
        }

        private double RandomDistribution(Random rand, double mean, double scale)
        {
            double randStdNormal;
            do
            {
                double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            } while (randStdNormal > 1 || randStdNormal < -1);
            
            double randNormal = mean + scale * randStdNormal; //random normal(mean,scale^2)            
            return randNormal;
        }

        private double RandomLengthDistribution(Random rand)  //Box-Muller transform használatával, min és max között
        {
            double randNormal;
            do
            {
                double u1 = 1.0 - rand.NextDouble();
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                randNormal = (lengthMean + Math.Min(lengthMean - lengthMin, lengthMax - lengthMean) * randStdNormal) / mmsPerPixel;
            } while (randNormal > lengthMax / mmsPerPixel || randNormal < lengthMin / mmsPerPixel);
            return randNormal;
        }

        public class SteelFiberLine
        {
            public List<Point3i> Segments { get; set; }   // az acélszál szakaszokból áll, amik közelítik a görbület létét
            public Point3i StartPoint { get; set; }
            public Point3i EndPoint { get; set; }
            public int TouchingFiber { get; set; } = -1; // ha értéke > 0; akkor a megegyező számúak összeérnek

            public SteelFiberLine(List<Point3i> segments, Point3i startPoint, Point3i endPoint)
            {
                Segments = segments ?? throw new ArgumentNullException(nameof(segments));
                StartPoint = startPoint;
                EndPoint = endPoint;
            }

            public SteelFiberLine()
            {
                Segments = new();
                StartPoint = new();
                EndPoint = new();
                TouchingFiber = -1;
            }
        }

        public class Blob
        {
            public int Id { get; set; }
            public int SteelFiberID { get; set; }
            public List<Point3i> Pixels { get; set; }

        }
    }
}
