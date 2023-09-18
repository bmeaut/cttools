using Core.Interfaces.Image;

namespace Core.Image
{
    /// <summary>
    /// Represents a key-value pair which can be put on blobs.
    /// </summary>
    public class Tag : ITag<int>
    {
        public string Name { get; set; }
        public int Value { get; set; }

        public Tag(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }
}
