using Core.Interfaces.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class QueryOperation : IOperation
    {
        public string Name => "Query";

        public OperationProperties DefaultOperationProperties =>
            new QueryOperationProperties();

        public Action<string> ActionToOutputInformationRow { get; set; }

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var blobImage = context.BlobImages[context.ActiveLayer];
            try
            {
                var blobIds = context.OperationRunEventArgs.Strokes
                    .SelectMany(pointArray => pointArray.Select(
                        point => blobImage[point.Y, point.X]))
                    .Distinct();
                foreach (int blobId in blobIds)
                {
                    ActionToOutputInformationRow($"----- BlobID = {blobId} -----");
                    foreach (var tag in blobImage.GetTagsForBlob(blobId))
                        ActionToOutputInformationRow(
                            $"{tag.Name}={tag.Value}");
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                ActionToOutputInformationRow("Query operation is probably used at invalid image location.");
            }
            return context;
        }

        public bool IsCallableFromButton(OperationProperties operationProperties) => false;

        public class QueryOperationProperties : OperationProperties
        {

        }
    }
}
