using Cv4s.Common.Models;
using Cv4s.Operations.PoreSizeStatOperation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cv4s.Operations.PoreSizeStatOperation
{
    public class PoreSizeStatOperationProperties : OperationProperties
    {
        public string HistogramName { get; set; } = "PoreSizeHistogram";

        public double MinSieveSize { get; set; } = 0.25;

        public AlgorithmOption Algorithm { get; set; } = AlgorithmOption.Simple;
    }
}
