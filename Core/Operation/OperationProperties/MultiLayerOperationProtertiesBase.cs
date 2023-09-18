using Core.Interfaces.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class MultiLayerOperationProtertiesBase : OperationProperties
    {
        public bool RunBatch { get; set; }
    }
}
