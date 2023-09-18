using Core.Interfaces.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class DistanceMeasurementOperation : IOperation
    {
        public string Name => "Distance measurement";

        public OperationProperties DefaultOperationProperties =>
            new DistanceMeasurementOperationProperties();

        public Action<string> ActionToOutputInformationRow { get; set; }


        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            foreach (var stroke in context.OperationRunEventArgs.Strokes)
            {
                var a = stroke.First();
                var b = stroke.Last();
                var distanceInMmX = (b.X - a.X) * context.RawImageMetadata.XResolution;
                var distanceInMmY = (b.Y - a.Y) * context.RawImageMetadata.YResolution;
                var distance
                    = Math.Sqrt(distanceInMmX * distanceInMmX
                        + distanceInMmY * distanceInMmY);
                ActionToOutputInformationRow($"Distance: {distance}");
            }
            return context;
        }
    }
    public class DistanceMeasurementOperationProperties : OperationProperties { }
}
