using Cv4s.Common.Models.Images;
using OpenCvSharp;

namespace Cv4s.Common.Interfaces.Images
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

        void SetTags(IEnumerable<int> blobIds, string tagName, int value = 0);
           

        void RemoveTag(int blobId, string tagName);

        void RemoveTags(IEnumerable<int> blobIds, string tagName);
            

        IEnumerable<ITag<int>> GetTagsForBlob(int blobId);
    }
}
