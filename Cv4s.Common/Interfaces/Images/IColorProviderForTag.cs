using OpenCvSharp;

namespace Cv4s.Common.Interfaces.Images
{
    public interface IColorProviderForTag
    {
        public Vec4b GetColor(ITag<int> tag);
    }
}
