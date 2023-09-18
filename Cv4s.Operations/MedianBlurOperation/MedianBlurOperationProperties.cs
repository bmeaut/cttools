using Cv4s.Common.Enums;
using Cv4s.Common.Models;

namespace Cv4s.Operations.RoiOperation
{
    public class MedianBlurOperationProperties : OperationProperties
    {
        public static string DefaultFolderName = "BluredImages";
        public enum KernelSize
        {
            Three = 3,
            Five = 5,
            Seven = 7,
            Eleven = 11,
            Thriteen = 13,
            Fifteen = 15,
        };

        public string FolderName { get; set; } = DefaultFolderName;
        public int IterationNumberPerImage { get; set; } = 3;
        public KernelSize KSize { get; set; } = KernelSize.Five;
    }
}
