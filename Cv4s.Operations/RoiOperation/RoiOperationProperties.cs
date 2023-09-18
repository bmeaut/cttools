using Cv4s.Common.Enums;
using Cv4s.Common.Models;

namespace Cv4s.Operations.RoiOperation
{
    public class RoiOperationProperties : IterableOperationProperties
    {
        public ModeEnum Mode { get; set; } = ModeEnum.AutoMarkOuterBlobsToRemove;
    }
}
