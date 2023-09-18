using Core.Model.SteelFibers;
using Core.SteelFibers.Generation;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Core.Test.SteelFibers
{
    public class SteelFiberReinforcedSampleDicomGeneratorTests
    {
        SteeFiberReinforcedSampleDicomGenerator generator = new("teszt.txt", "kepek", "kimenet");

        [Fact]
        public void GetNewCentersTest()
        {
            List<Mat> images = new();
            for (int i = 0; i < 10; i++)
            {
                var image = new Mat(400, 400, MatType.CV_8UC3);
                Cv2.Rectangle(image, new Point(0, 0), new Point(400, 400), new Scalar(0, 0, 0), -1);
                images.Add(image);
            }
            SteeFiberReinforcedSampleDicomGenerator.SteelFiberLine steelFiber = new();
            steelFiber.Segments = new();
            steelFiber.Segments.Add(new Point3i(100, 100, 2));
            steelFiber.Segments.Add(new Point3i(100, 100, 3));
            steelFiber.Segments.Add(new Point3i(100, 100, 6));

            var centers = generator.GetNewCenters(images, steelFiber);
            List<Point3i> expected = new();
            expected.Add(new Point3i(100, 100, 2));
            expected.Add(new Point3i(100, 100, 3));
            expected.Add(new Point3i(100, 100, 4));
            expected.Add(new Point3i(100, 100, 5));

            Assert.Equal(expected, centers);
        }

        [Fact]
        public void GetFiberIdsTest()
        {
            List<SteeFiberReinforcedSampleDicomGenerator.Blob> blobs = new();
            blobs.Add(new SteeFiberReinforcedSampleDicomGenerator.Blob() { SteelFiberID = 1, Pixels = new List<Point3i>() { new Point3i(100, 100, 4) } });
            blobs.Add(new SteeFiberReinforcedSampleDicomGenerator.Blob() { SteelFiberID = 2, Pixels = new List<Point3i>() { new Point3i(100, 100, 2) } });
            blobs.Add(new SteeFiberReinforcedSampleDicomGenerator.Blob() { SteelFiberID = 2, Pixels = new List<Point3i>() { new Point3i(100, 100, 3) } });

            List<SteeFiberReinforcedSampleDicomGenerator.SteelFiberLine> steelFibers = new();
            steelFibers.Add(new SteeFiberReinforcedSampleDicomGenerator.SteelFiberLine());
            steelFibers.Add(new SteeFiberReinforcedSampleDicomGenerator.SteelFiberLine());

            var ids = generator.GetFiberIds(blobs, steelFibers);
            List<int> expected = new List<int>() { 1, 0 };

            Assert.Equal(expected, ids);
        }

        [Fact]
        public void CreateSoluitonTest()
        {
            List<Mat> images = new();
            for (int i = 0; i < 10; i++)
            {
                var image = new Mat(400, 400, MatType.CV_8UC3);
                Cv2.Rectangle(image, new Point(0, 0), new Point(400, 400), new Scalar(0, 0, 0), -1);
                images.Add(image);
            }
            var indexer = images[2].GetGenericIndexer<Vec3b>();
            indexer[100, 100] = new Vec3b(1, 1, 1);
            indexer = images[3].GetGenericIndexer<Vec3b>();
            indexer[100, 100] = new Vec3b(1, 1, 1);
            indexer = images[4].GetGenericIndexer<Vec3b>();
            indexer[200, 200] = new Vec3b(1, 1, 1);

            List<SteeFiberReinforcedSampleDicomGenerator.Blob> blobs = new();
            blobs.Add(new SteeFiberReinforcedSampleDicomGenerator.Blob() { SteelFiberID = 1, Pixels = new List<Point3i>() { new Point3i(200, 200, 4) } });
            blobs.Add(new SteeFiberReinforcedSampleDicomGenerator.Blob() { SteelFiberID = 0, Pixels = new List<Point3i>() { new Point3i(100, 100, 2) } });
            blobs.Add(new SteeFiberReinforcedSampleDicomGenerator.Blob() { SteelFiberID = 0, Pixels = new List<Point3i>() { new Point3i(100, 100, 3) } });

            List<SteeFiberReinforcedSampleDicomGenerator.SteelFiberLine> steelFibers = new();
            steelFibers.Add(new SteeFiberReinforcedSampleDicomGenerator.SteelFiberLine());
            steelFibers.Add(new SteeFiberReinforcedSampleDicomGenerator.SteelFiberLine());

            var fibers = generator.CreateSolution(images, blobs, steelFibers);
            Dictionary<int, int> blobDict1 = new();
            blobDict1.Add(2, 2);
            blobDict1.Add(3, 2);
            Dictionary<int, int> blobDict2 = new();
            blobDict2.Add(4, 2);
            List<SteelFiber> expected = new() { new SteelFiber() { SteelFiberId = 0, Blobs = blobDict1 }, new SteelFiber() { SteelFiberId = 1, Blobs = blobDict2 } };

            Assert.Equal(expected.Count, fibers.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].Blobs, fibers[i].Blobs);
                Assert.Equal(expected[i].SteelFiberId, fibers[i].SteelFiberId);
            }
        }

        [Fact]
        public void PointsAreCloseTest()
        {
            List<Point3i> centers = new();
            centers.Add(new Point3i(100, 100, 10));
            centers.Add(new Point3i(100, 100, 11));
            List<Point3i> centersNew = new();
            centersNew.Add(new Point3i(105, 105, 10));
            centersNew.Add(new Point3i(105, 115, 11));

            var result = generator.CloserToOtherFibersThanDist(centers, centersNew, 30);
            Assert.True(result);
        }

        [Fact]
        public void PointsAreNotCloseTest()
        {
            List<Point3i> centers = new();
            centers.Add(new Point3i(100, 100, 10));
            centers.Add(new Point3i(100, 100, 11));
            List<Point3i> centersNew = new();
            centersNew.Add(new Point3i(105, 135, 10));
            centersNew.Add(new Point3i(105, 145, 11));

            var result = generator.CloserToOtherFibersThanDist(centers, centersNew, 30);
            Assert.False(result);
        }

        [Fact]
        public void NormalizeAngleCorrectTest()
        {
            double angle = 60;
            double max = 180;
            var result = generator.NormalizeAngle(angle, max);

            Assert.Equal(angle, result);
        }

        [Fact]
        public void NormalizeAngleBiggerTest()
        {
            double angle = 220;
            double max = 180;
            var result = generator.NormalizeAngle(angle, max);

            Assert.Equal(angle - max, result);
        }

        [Fact]
        public void NormalizeAngleSmallerTest()
        {
            double angle = -40;
            double max = 180;
            var result = generator.NormalizeAngle(angle, max);

            Assert.Equal(angle + max, result);
        }

        [Fact]
        public void GeneratreTouchingSteelFiberPairTest()
        {
            SteeFiberReinforcedSampleDicomGenerator.SteelFiberLine steelFiber1, steelFiber2;
            (steelFiber1, steelFiber2) = generator.GenerateTouchingSteelFibersPair(new Random());

            int numberOfSamePoints = 0;
            for (int i = 0; i < steelFiber1.Segments.Count; i++)
            {
                Point3i segment1 = steelFiber1.Segments[i];
                Point3i segment2 = steelFiber2.Segments[i];
                if (segment1 == segment2)
                {
                    numberOfSamePoints++;
                }
            }
            Assert.Equal(1, numberOfSamePoints);
        }
    }
}
