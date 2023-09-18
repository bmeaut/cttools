using System.Collections.Generic;
using System.Linq;

namespace Core.Image
{
    public class Blob
    {
        public Blob(int blobId)
        {
            BlobId = blobId;
        }

        public int BlobId { get; set; }

        private readonly List<Tag> tags = new List<Tag>();
        public IEnumerable<Tag> Tags => tags;

        public void SetTag(string tagName, int value = 0)
        {
            if (!tags.Any(t => t.Name == tagName))
                tags.Add(new Tag(tagName, value));
            else
                tags.Single(t => t.Name == tagName).Value = value;
        }

        public bool HasTag(string tagName)
        {
            return tags.Any(t => t.Name == tagName);
        }

        public bool HasTag(string tagName, int value)
        {
            return tags.Any(t => (t.Name == tagName && t.Value == value));
        }

        public void RemoveTag(string tagName)
        {
            tags.RemoveAll(t => t.Name == tagName);
        }
    }
}