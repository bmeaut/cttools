using Cv4s.Common.Models;

namespace Cv4s.Operations.MultiChannelThresoldOperation
{
    public class MultiChannelThresholdOperationProperties : OperationProperties
    {
        public List<int> TresholdValues { get; set; } = new List<int>() { 50, 150 };
        public bool RunParallel { get; set; } = true;
    }
}
