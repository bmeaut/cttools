using Cv4s.Common.Interfaces.Images;

namespace Cv4s.Operations.PoreSizeStatOperation.Models
{
    public interface IPoreSizeAlgorithm
    {
        Dictionary<int, SizeVolume> Calculate(Dictionary<int, List<LayerPore>> poresOnEachLayer,
            IBlobImageSource blobImages,
            double ResX,
            double ResY,
            double ResZ);
    }
}
