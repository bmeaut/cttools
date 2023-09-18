using Cv4s.Common.Interfaces.Images;
using OpenCvSharp;

namespace Cv4s.Common.Extensions
{
    public static class IBlobImageExtensionMethods
    {
        /// <summary>
        /// Real blobID: 0 is not considered to be a real blob
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<int> CollectAllRealBlobIds(this IBlobImage blobImage)
        {
            var presentBlobIds = new HashSet<int>();
            for (int y = 0; y < blobImage.Size.Height; y++)
                for (int x = 0; x < blobImage.Size.Width; x++)
                    presentBlobIds.Add(blobImage[y, x]);
            presentBlobIds.Remove(0);
            return presentBlobIds;
        }

        public static int GetNextUnusedBlobId(this IBlobImage blobImage)
        {
            return blobImage.CollectAllRealBlobIds()
                .OrderBy(i => i).LastOrDefault() + 1;
        }

        public static void DrawBlobRect(this IBlobImage blobImage, int x, int y, int w, int h, int blobId)
        {
            DrawBlobRect(blobImage, new Rect(x, y, w, h), blobId);
        }

        public static void DrawBlobRect(this IBlobImage blobImage, Rect rect, int blobId)
        {
            var mask = blobImage.GetEmptyMask();
            mask.Rectangle(rect, new Scalar(255), -1);
            blobImage.SetBlobMask(mask, blobId);
        }

        public static IEnumerable<int> GetBlobIdsHitByMask(this IBlobImage blobImage, Mat mask)
        {
            HashSet<int> ids = new HashSet<int>();
            var indexer = mask.GetGenericIndexer<byte>();
            for (int y = 0; y < mask.Rows; y++)
                for (int x = 0; x < mask.Cols; x++)
                {
                    if (indexer[y, x] > 0 && blobImage[y, x] != 0)
                        ids.Add(blobImage[y, x]);
                }
            return ids;
        }

        public static int? GetTagValueOrNull(this IBlobImage blobImage, int blobId, string tagName)
        {
            return blobImage.GetTagsForBlob(blobId).Where(
                t => t.Name == tagName).Select(t => (int?)t.Value).SingleOrDefault();
        }

        public static int[] GetHitBlobIds(this IBlobImage blobImage, Point[][] strokes)
        {
            return strokes.SelectMany(s => s).Select(p => blobImage[p.Y, p.X])
                .Distinct().ToArray();
        }

        public static Mat GetMaskUnion(this IBlobImage blobImage, IEnumerable<int> blobIds)
        {
            var unionMask = new Mat(blobImage.Size, MatType.CV_8UC1, 0);
            var unionMaskIndexer = unionMask.GetGenericIndexer<byte>();
            var blobIdsSet = new HashSet<int>(blobIds);
            for (int x = 0; x < unionMask.Width; x++)
            {
                for (int y = 0; y < unionMask.Height; y++)
                {
                    var blobId = blobImage[y, x];
                    if (blobIdsSet.Contains(blobId))
                    {
                        unionMaskIndexer[y, x] = 255;
                    }
                }
            }
            return unionMask;
        }
    }
}
