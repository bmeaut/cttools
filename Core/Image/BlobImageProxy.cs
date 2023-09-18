using Core.Interfaces.Image;
using OpenCvSharp;
using System.Collections.Generic;
using System.Linq;

namespace Core.Image
{
    /// <summary>
    /// Note: negative BlobId values are not supported (alse used for internal purposes)
    /// Zero values are stored in reality as values BlobIdToMarkDelete due to the
    ///     nature of sprase matrices.
    /// </summary>
    public class BlobImageProxy : IBlobImage
    {
        private readonly BlobImageChangeSet changeset;

        public BlobImageProxy(BlobImage original)
        {
            changeset = new BlobImageChangeSet(original);
        }
        internal BlobImageProxy(BlobImageChangeSet changeset)
        {
            this.changeset = changeset;
        }

        public Size Size => changeset.Original.Size;

        public bool IsChanged { get; set; }

        public Dictionary<int, List<Tag>> Tags => changeset.Original.Tags;

        public int this[int y, int x]
        {
            get
            {
                //BlobImage.CheckCoordinatesAndThrowExceptionWhenOutOfRange(Size, x, y); // This makes the access very slow!
                int diff = changeset.DiffIndexer[y, x];
                if (diff == 0)
                    return changeset.Original[y, x];
                else if (diff != BlobImageChangeSet.BlobIdToMarkDelete)
                    return diff;
                else
                    return 0;
            }
            set
            {
                //BlobImage.CheckCoordinatesAndThrowExceptionWhenOutOfRange(Size, x, y); // This makes the access very slow!
                int diff = value == 0 ? BlobImageChangeSet.BlobIdToMarkDelete : value;
                changeset.DiffIndexer[y, x] = diff;
            }
        }

        public Mat GetEmptyMask()
            => changeset.Original.GetEmptyMask();

        public Mat GetMask(int blobId)
        {
            var mask = this.GetEmptyMask();
            var maskIndexer = mask.GetGenericIndexer<byte>();
            for (int y = 0; y < Size.Height; y++)
                for (int x = 0; x < Size.Width; x++)
                    if (this[y, x] == blobId)
                        maskIndexer[y, x] = 1;
            return mask;
        }

        public void SetBlobMask(Mat mask, int blobId, bool removeFromOtherLocations = false)
        {
            IsChanged = true;
            var setBlobId = (blobId != 0) ? blobId : BlobImageChangeSet.BlobIdToMarkDelete;
            var maskIndexer = mask.GetGenericIndexer<byte>();
            for (int y = 0; y < Size.Height; y++)
                for (int x = 0; x < Size.Width; x++)
                    if (maskIndexer[y, x] != 0)
                        this[y, x] = setBlobId;
                    else if (removeFromOtherLocations && this[y, x] == blobId)
                        this[y, x] = 0;
        }


        public void SetTag(int blobId, string tagName, int value = 0)
        {
            IsChanged = true;
            if (!HaveTagChange(blobId, tagName))
            {
                changeset.TagChanges.Add(
                    new BlobImageChangeSet.TagChange()
                    {
                        BlobId = blobId,
                        TagName = tagName,
                        ValueOrNullToRemove = value
                    });
            }
            else
            {
                changeset.TagChanges.Where(tc => tc.BlobId == blobId &&
                    tc.TagName == tagName).Single().ValueOrNullToRemove = value;
            }
        }

        public IEnumerable<int> GetBlobsByTagValue(string tagName, int? value = null)
        {
            var allBlobIds = changeset.TagChanges.Select(tc => tc.BlobId)
                .Concat(changeset.Original.AllBlobIdsHavingTags)
                .Distinct().ToArray();
            foreach (var b in allBlobIds)
            {
                var tags = GetTagsForBlob(b);
                if (tags.Any(t => t.Name == tagName
                    && (value == null || t.Value == value)))
                {
                    yield return b;
                }
            }
        }

        public void RemoveTag(int blobId, string tagName)
        {
            IsChanged = true;
            if (HaveTagChange(blobId, tagName))
            {
                var tagChange = changeset.TagChanges.Single(
                    tc => tc.BlobId == blobId && tc.TagName == tagName);
                tagChange.ValueOrNullToRemove = null;
            }
            else
            {
                changeset.TagChanges.Add(new BlobImageChangeSet.TagChange() { BlobId = blobId, TagName = tagName, ValueOrNullToRemove = null });
            }
        }

        public IEnumerable<ITag<int>> GetTagsForBlob(int blobId)
        {
            ITag<int>[] originalTags = new ITag<int>[0];
            if (changeset.Original.HasBlobInTagDictionary(blobId))
                originalTags = changeset.Original.GetTagsForBlob(blobId).ToArray();

            var tagNames = originalTags
                .Select(t => t.Name).Concat(
                    changeset.TagChanges.Where(tc => tc.BlobId == blobId).Select(tc => tc.TagName)
                ).ToArray();
            foreach (var tagname in tagNames)
            {
                if (HaveTagChange(blobId, tagname))
                {
                    int? overriddenValue = GetTagChange(blobId, tagname);
                    if (overriddenValue.HasValue)
                        yield return new Tag(tagname, overriddenValue.Value);
                }
                else
                {
                    yield return originalTags.Single(t => t.Name == tagname);
                }
            }
        }

        private bool HaveTagChange(int blobId, string tagName)
        {
            return changeset.TagChanges.Any(tc => tc.BlobId == blobId
                && tc.TagName == tagName);
        }

        private int? GetTagChange(int blobId, string tagName)
        {
            return changeset.TagChanges.Single(tc => tc.BlobId == blobId
                && tc.TagName == tagName).ValueOrNullToRemove;
        }

        public BlobImageMemento ApplyToOriginal()
        {
            return changeset.ApplyToOriginal();
        }
    }
}
