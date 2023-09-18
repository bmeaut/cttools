using Core.Image;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Core.Interfaces.Image
{
    public interface IBlobImage
    {
        public int this[int y, int x] { get; set; }
        bool IsChanged { get; set; }
        Dictionary<int, List<Tag>> Tags
        {
            get;
        }


        Mat GetEmptyMask();
        Mat GetMask(int blobId);
        void SetBlobMask(Mat mask, int blobId, bool removeFromOtherLocations = false);
        Size Size { get; }

        IEnumerable<int> GetBlobsByTagValue(string tagName, int? value);

        void SetTag(int blobId, string tagName, int value = 0);
        void SetTag(IEnumerable<int> blobIds, string tagName, int value = 0) => 
            blobIds.ToList().ForEach(b => SetTag(b, tagName, value));

        void RemoveTag(int blobId, string tagName);
        void RemoveTag(IEnumerable<int> blobIds, string tagName) =>
            blobIds.ToList().ForEach(b => RemoveTag(b, tagName));

        IEnumerable<ITag<int>> GetTagsForBlob(int blobId);
    }
}
