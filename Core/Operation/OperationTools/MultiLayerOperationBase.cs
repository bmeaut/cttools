using Core.Interfaces.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public abstract class MultiLayerOperationBase : IOperation
    {
        public abstract string Name { get; }

        public abstract OperationProperties DefaultOperationProperties { get; }

        public virtual async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var operationProperties = (MultiLayerOperationProtertiesBase)context.OperationProperties;
            int done = 0;
            progress.Report(done);
            if (operationProperties.RunBatch)
                foreach (var layerID in context.BlobImages.Keys)
                {
                    if (token.IsCancellationRequested) return null;
                    context = await RunOneLayer(context, layerID);
                    progress.Report(done++ / (double)context.BlobImages.Keys.Count);
                }

            if (!operationProperties.RunBatch)
                context = await RunOneLayer(context, context.ActiveLayer);
            progress.Report(1);
            return context;
        }

        public abstract Task<OperationContext> RunOneLayer(OperationContext context, int layer);
    }
}
