using Cv4s.Common.Enums;
using Cv4s.Common.Models;

namespace Cv4s.Operations.ConnectedComponents3DOperation
{
    public class ConnectedComponents3DOperationProperties : OperationProperties
    {
        public MaterialTag Material { get; set; } = MaterialTag.PORE;
    }
}
