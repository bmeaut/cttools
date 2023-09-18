using Core.Interfaces.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    abstract public class MultiLayerParallelOperationBase : IOperation
    {
        public abstract string Name { get; }

        public abstract OperationProperties DefaultOperationProperties { get; }

        public virtual async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var operationProperties = (MultiLayerOperationProtertiesBase)context.OperationProperties;
            progress.Report(0);
            if (operationProperties.RunBatch)
            {
                int layerCount = context.BlobImages.Keys.Count;
                var previousProgressValues = new double[layerCount];
                double progressSum = 0;
                Parallel.ForEach(context.BlobImages.Keys, layerID =>
                {
                    if (token.IsCancellationRequested) return;
                    var layerProgress = new Progress<double>();
                    layerProgress.ProgressChanged += (_, p) =>
                    {
                        AddToProgressSum(ref progressSum, p - previousProgressValues[layerID]);
                        previousProgressValues[layerID] = p;
                        progress.Report(progressSum / layerCount);
                    };
                    RunOneLayer(context, layerID, layerProgress, token);
                    AddToProgressSum(ref progressSum, 1 - previousProgressValues[layerID]);
                    progress.Report(progressSum / layerCount);
                });
            }

            if (!operationProperties.RunBatch)
                await RunOneLayer(context, context.ActiveLayer, progress, token);
            progress.Report(1);
            return context;
        }

        private void AddToProgressSum(ref double progressSum, double value)
            => Interlocked.Exchange(ref progressSum, progressSum + value);

        public abstract Task RunOneLayer(OperationContext context, int layer, IProgress<double> progress, CancellationToken token);

    }
}
