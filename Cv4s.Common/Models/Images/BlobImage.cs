using Cv4s.Common.Extensions;
using Cv4s.Common.Interfaces.Images;
using OpenCvSharp;

namespace Cv4s.Common.Models.Images
{
    public class BlobImage : IBlobImage
    {
        private readonly Mat Image;
        private readonly Mat.Indexer<int> Indexer;
        public bool IsChanged { get; set; }

        public BlobImage(Size size)
        {
            Image = new Mat(size, MatType.CV_32SC1);
            Image.SetTo(0);
            Indexer = Image.GetGenericIndexer<int>();
        }

        public BlobImage(int width, int height)
            : this(new Size(width, height))
        {
        }

        #region Access to the image
        public int this[int y, int x]
        {
            get
            {
                CheckCoordinatesAndThrowExceptionWhenOutOfRange(Size, x, y);
                return Indexer[y, x];
            }
            set
            {
                CheckCoordinatesAndThrowExceptionWhenOutOfRange(Size, x, y);
                Indexer[y, x] = value;
                AddTagDictionaryKeyIfNeeded(value);
            }
        }

        public static void CheckCoordinatesAndThrowExceptionWhenOutOfRange(
            Size size, int x, int y)
        {
            if (y < 0 || y >= size.Height)
                throw new ArgumentOutOfRangeException(nameof(y), y, $"Valid: 0-{size.Height - 1}");
            else if (x < 0 || x >= size.Width)
                throw new ArgumentOutOfRangeException(nameof(x), x, $"Valid: 0-{size.Width - 1}");
        }
        #endregion

        public Size Size => Image.Size();

        #region Blob and mask operations
        public bool HasBlobInTagDictionary(int blobId)
            => Tags.ContainsKey(blobId);

        public Mat GetEmptyMask()
        {
            return new Mat(this.Size, MatType.CV_8UC1, new Scalar(0));
        }

        /// <summary>
        /// Returns a mask image (CV_8UC1) where locations of the given blob ID are marked.
        /// </summary>
        /// <param name="blobId">The ID of the label to look for.</param>
        /// <returns>Mask image (CV_8UC1) indicating the locations of the blob with nonzero values.</returns>
        public Mat GetMask(int blobId)
        {
            var mask = new Mat(this.Image.Size(), MatType.CV_8UC1);
            Cv2.Compare(this.Image, blobId, mask, CmpType.EQ);
            return mask;
        }

        /// <summary>
        /// Stores a blob defined by a mask image with given blob ID.
        /// </summary>
        /// <param name="mask">8UC1 mask image with nonzero values at the pixels of the blob.</param>
        /// <param name="blobId">The ID to use for the blob.</param>
        public void SetBlobMask(Mat mask, int blobId, bool removeFromOtherLocations = false)
        {
            IsChanged = true;
            if (removeFromOtherLocations)
            {
                var previousMask = GetMask(blobId);
                SetBlobMask(previousMask, 0, false);
            }
            this.Image.SetTo(blobId, mask);
            AddTagDictionaryKeyIfNeeded(blobId);
        }

        private void AddTagDictionaryKeyIfNeeded(int blobId)
        {
            if (blobId > 0 && !Tags.ContainsKey(blobId))
                Tags.Add(blobId, new List<Tag>());
        }

        public void RemoveUnusedTagDictionaryKeys()
        {
            var presentBlobIds = this.CollectAllRealBlobIds().ToArray();

            foreach (var blobId in Tags.Keys.ToArray())
                if (!presentBlobIds.Contains(blobId))
                    Tags.Remove(blobId);
        }
        #endregion

        #region BGRA image generation
        /// <summary>
        /// Generates a color image based on the blobs and a blobID->color converter.
        /// </summary>
        /// <param name="colorImage">A BGRA CV_8UC4 image to write to, already allocated.</param>
        /// <param name="converter">Provides the mapping between blob IDs and colors.</param>
        public void GenerateBGRAImage(Mat colorImage, IBlobAppearanceService converter)
        {
            converter.PrepareBlobs(this);
            var colorImageIndexer = colorImage.GetGenericIndexer<Vec4b>();
            for (int y = 0; y < this.Image.Rows; y++)
                for (int x = 0; x < this.Image.Cols; x++)
                    colorImageIndexer[y, x] = converter[this.Indexer[y, x]];
        }

        /// <summary>
        /// Convenience wrapper if you do not want to reuse a colorImage.
        /// </summary>
        /// <param name="converter"></param>
        /// <returns></returns>
        public Mat GenerateBGRAImage(IBlobAppearanceService converter)
        {
            Mat colorImage = new Mat(this.Size, MatType.CV_8UC4);
            GenerateBGRAImage(colorImage, converter);
            return colorImage;
        }
        #endregion

        #region Tag operations
        private readonly Dictionary<int, List<Tag>> tags = new Dictionary<int, List<Tag>>();
        public Dictionary<int, List<Tag>> Tags => tags;

        public IEnumerable<int> GetBlobsByTagValue(string tagName, int? value = null)
            => Tags.Where(t => TagListHasTagValue(t.Value, tagName, value)).Select(t => t.Key);

        private bool TagListHasTagValue(List<Tag> list, string tagName, int? value = null)
            => list.Any(t => t.Name == tagName && (value == null || t.Value == value.Value));

        public IEnumerable<ITag<int>> GetTagsForBlob(int blobId)
            => Tags[blobId];

        public void RemoveTag(int blobId, string tagName)
        {
            IsChanged = true;
            Tags[blobId].RemoveAll(t => t.Name == tagName);
        }

        public void SetTag(int blobId, string tagName, int value = 0)
        {
            if (blobId == 0)
                return;

            IsChanged = true;
            Tag alreadyPresentTag = Tags[blobId].Where(t => t.Name == tagName).SingleOrDefault();
            if (alreadyPresentTag != null)
                alreadyPresentTag.Value = value;
            else
                Tags[blobId].Add(new Tag(tagName, value));
        }

        public void RemoveTags(IEnumerable<int> blobIds, string tagName)
            => blobIds.ToList().ForEach(b => RemoveTag(b, tagName));

        public void SetTags(IEnumerable<int> blobIds, string tagName, int value = 0)
            => blobIds.ToList().ForEach(b => SetTag(b, tagName, value));


        public IEnumerable<int> AllBlobIdsHavingTags => Tags.Keys;
        #endregion
    }
}
