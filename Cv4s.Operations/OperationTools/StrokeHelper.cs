using Cv4s.Common.Extensions;
using Cv4s.Common.Interfaces.Images;
using OpenCvSharp;

namespace Cv4s.Operations.OperationTools
{
    public static class StrokeHelper
    {
        public static void Delete(IBlobImage blobImage, Point[][] strokes)
        {
            var blobsToDelete = GetBlobIdsHitByStrokePoints(blobImage, strokes);
            for (int y = 0; y < blobImage.Size.Height; y++)
                for (int x = 0; x < blobImage.Size.Width; x++)
                    if (blobsToDelete.Contains(blobImage[y, x]))
                        blobImage[y, x] = 0;
        }

        public static IEnumerable<int> GetBlobIdsHitByStrokePoints(IBlobImage blobImage, Point[][] strokes)
        {
            var blobIdsHitByStrokeControlPoints = new SortedSet<int>();
            foreach (Point[] stroke in strokes)
                foreach (Point p in stroke)
                    blobIdsHitByStrokeControlPoints.Add(blobImage[p.Y, p.X]);
            return blobIdsHitByStrokeControlPoints;
        }



        public static void Add(IBlobImage blobImage, int id, bool isStrokeClosed, Point[][] strokes)
        {
            // Add new closed area as new blob
            var mask = GetMaskForStrokes(blobImage, strokes, isStrokeClosed);
            blobImage.SetBlobMask(mask, id);
        }

        public static void Subtract(IBlobImage blobImage, bool isStrokeClosed, Point[][] strokes)
        {
            // Subtract the closed area as from all blobs
            var mask = GetMaskForStrokes(blobImage, strokes, isStrokeClosed);
            blobImage.SetBlobMask(mask, 0);
        }

        private static readonly Scalar White255 = new Scalar(255);
        public static Mat GetMaskForStrokes(IBlobImage blobImage, Point[][] strokes, bool isClosed)
        {
            var mask = blobImage.GetEmptyMask();
            return AddStrokesToMask(mask, strokes, isClosed);
        }

        public static Mat AddStrokesToMask(Mat mask, Point[][] strokes, bool isClosed)
        {
            if (isClosed)
                Cv2.FillPoly(mask, strokes, White255);
            else
                Cv2.Polylines(mask, strokes, false, White255);
            return mask;
        }

        public static Mat GetMaskForStroke(IBlobImage blobImage, Point[] stroke, bool isClosed)
            => GetMaskForStrokes(blobImage, new Point[][] { stroke }, isClosed);

        public static void Extend(IBlobImage blobImage, bool isStrokeClosed, Point[][] strokes)
        {
            foreach (Point[] stroke in strokes)
            {
                var start = stroke[0];
                var blobId = blobImage[start.Y, start.X];
                var mask = GetMaskForStroke(blobImage, stroke, isStrokeClosed);
                blobImage.SetBlobMask(mask, blobId);
            }
        }

        public static void Slice(IBlobImage blobImage, Point[][] strokes)
        {
            var mask = GetMaskForStrokes(blobImage, strokes, false);
            var blobIdsWhichMayBeSeparated = blobImage.GetBlobIdsHitByMask(mask);
            blobImage.SetBlobMask(mask, 0);
            foreach (int blobId in blobIdsWhichMayBeSeparated)
                ConnectedComponentsHelper.SeparateSegmentedPartsOfBlob(
                    blobImage, blobId);
        }
    }
}
