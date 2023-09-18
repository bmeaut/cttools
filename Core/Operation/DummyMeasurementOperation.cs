using Core.Interfaces.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class DummyMeasurementOperation : IOperation
    {
        public string Name => "DummyMeasurementOperation";

        public OperationProperties DefaultOperationProperties => new EmptyOperationProperties();

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            // TODO: operation properties type validation
            var operationProperties = context.OperationProperties;

            var activeLayer = context.ActiveLayer;

            var output = OxyplotInternalOutput.CreateScatterPlot(
                "Best Dummy title", "time", "efficiency",
                new int[] { 1, 2, 5, 6, 7 },
                new int[] { 10, 20, 30, 40, 40 }
                );
            string OutputName = "DummyMeasurementOutput";
            context.AddInternalOutput(OutputName, output);
            //output.ExportToPngFile("oxyplot_export.png");

            return context;
        }
        public bool IsCallableFromCanvas(OperationProperties operationProperties) => false;

    }
}
