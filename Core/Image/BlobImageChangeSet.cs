using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Image
{
    /// <summary>
    /// Represents changes in BlobImage.
    /// Used by proxy and memento patterns.
    /// </summary>
    class BlobImageChangeSet
    {
        public readonly BlobImage Original;
        public readonly SparseMat DiffImage;
        public readonly SparseMatIndexer<int> DiffIndexer;

        public const int BlobIdToMarkDelete = -1;  // cannot store 0 in SparseMat

        public class TagChange
        {
            public int BlobId;
            public string TagName;  // Can be null to indicate tagless blob added inside proxy
            public int? ValueOrNullToRemove;
        }

        public readonly List<TagChange> TagChanges =
            new List<TagChange>();

        public BlobImageChangeSet(BlobImage original)
        {
            this.Original = original;
            DiffImage = new SparseMat(
                new int[] { original.Size.Height, original.Size.Width },
                MatType.CV_32SC1);
            DiffIndexer = DiffImage.GetIndexer<int>();
        }

        internal bool IsIdentity =>
            !((DiffImage.NzCount() != 0) || (TagChanges.Count > 0));

        public BlobImageMemento ApplyToOriginal()
        {
            var inverseChangeSet = new BlobImageChangeSet(Original);

            for (int y = 0; y < Original.Size.Height; y++)
                for (int x = 0; x < Original.Size.Width; x++)
                    if (DiffIndexer[y, x] != 0)
                    {
                        StoreInverseBlobIdChange(inverseChangeSet, x, y);
                        ApplyBlobIdChange(x, y);
                    }

            foreach (var tc in TagChanges)
            {
                StoreInverseTagChange(inverseChangeSet, tc);
                ApplyTagChange(tc);
            }

            return new BlobImageMemento(inverseChangeSet);
        }

        private void StoreInverseTagChange(
            BlobImageChangeSet inverseChangeSet, TagChange tc)
        {
            var originalAffedtedTag =
                Original.GetTagsForBlob(tc.BlobId)
                .SingleOrDefault(t => t.Name == tc.TagName);
            inverseChangeSet.TagChanges.Add(
                new TagChange()
                {
                    TagName = tc.TagName,
                    BlobId = tc.BlobId,
                    ValueOrNullToRemove = originalAffedtedTag?.Value
                });
        }

        private void ApplyTagChange(TagChange tc)
        {
            if (tc.ValueOrNullToRemove.HasValue)
            {
                Original.SetTag(tc.BlobId, tc.TagName, tc.ValueOrNullToRemove.Value);
            }
            else
            {
                Original.RemoveTag(tc.BlobId, tc.TagName);
            }
        }

        private void ApplyBlobIdChange(int x, int y)
        {
            if (DiffIndexer[y, x] != BlobIdToMarkDelete)
            {
                Original[y, x] = DiffIndexer[y, x];
            }
            else
            {
                Original[y, x] = 0;
            }
        }

        private void StoreInverseBlobIdChange(BlobImageChangeSet inverseChangeSet, int x, int y)
        {
            if (Original[y, x] != 0)
            {
                inverseChangeSet.DiffIndexer[y, x] = Original[y, x];
            }
            else
            {
                inverseChangeSet.DiffIndexer[y, x] = BlobIdToMarkDelete;
            }
        }
    }
}
