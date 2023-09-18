using Cv4s.Common.Enums;
using Cv4s.Common.Models;

namespace Cv4s.Operations.SelectMaterialsOperation
{
    public class SelectMaterialsOperationProperties : IterableOperationProperties
    {
        public MaterialTag MaterialTag { get; set; } = MaterialTag.PORE;
        public bool Remove { get; set; } = false;

    }
}
