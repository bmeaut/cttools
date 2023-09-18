using OpenCvSharp;

namespace Core.Interfaces.Image
{
    /// <summary>
    /// Interface to assign color to blobs (usually based on their tags).
    /// </summary>
    public interface IBlobId2ColorConverterService
    {
        /// <summary>
        /// Call this before iterating over the blobID-s in the image.
        /// </summary>
        /// <param name="allBlobs"></param>
        public abstract void PrepareBlobs(IBlobImage blobImage);
        public abstract Vec4b this[int blobId] { get; }

        public Vec4b DefaultBlobColor { get; set; }
        public bool AssignRandomDefaultColors { get; set; }

        /// <summary>
        /// Define a color and a priority for a Tag defined by its tagName
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="color"></param>
        /// <param name="priority">Higher number has bigger priority</param>
        public void AddTagAppearanceCommand(string tagName, Vec4b color, int priority);

        public void AddTagAppearanceCommand(ITagAppearanceCommand tagAppearanceCommand);

        public void SelectAppearance(int id);

    }
}
