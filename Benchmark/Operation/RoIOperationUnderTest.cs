using BenchmarkDotNet.Attributes;
using Core.Interfaces.Operation;
using Core.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Core.Operation.MultiChannelThresholdingOperation;

namespace Benchmark.Operation
{
    public class RoIOperationUnderTest : OperationUnderTestBase
    {
        private OperationContext context1;
        private OperationContext context2;

        private readonly CancellationToken token = new CancellationToken();
        IOperation operation;

        public RoIOperationUnderTest() : base()
        {
            var task = RunPrequisites();
            task.Wait();

            operation = new RoIOperation();
            var operationProperties1 = new RoIOperationProperties { Mode = RoIOperationProperties.ModeEnum.AutoMarkOuterBlobsToRemove };
            var operationProperties2 = new RoIOperationProperties { Mode = RoIOperationProperties.ModeEnum.RemoveMarkedAreasFromRoi };

            context1 = new OperationContext
            {
                OperationProperties = operationProperties1,
                BlobImages = BlobImages,
                RawImages = RawImages,
            };
            context2 = new OperationContext
            {
                OperationProperties = operationProperties2,
                BlobImages = BlobImages,
                RawImages = RawImages,
            };
        }

        private async Task RunPrequisites()
        {
            var operationProperties = new MultiChannelThresholdingOperationProperties { RunBatch = true, TresholdValues = new List<int> { 50, 150 } };
            var context = new OperationContext
            {
                OperationProperties = operationProperties,
                BlobImages = BlobImages,
                RawImages = RawImages,
            };
            IOperation op = new MultiChannelThresholdingOperation();
            var res = await op.Run(context);
            BlobImages = res.BlobImages;
        }

        [Benchmark]
        public async Task RunTest()
        {
            context1 = await operation.Run(context1);
            context2.BlobImages = context1.BlobImages;
            context2 = await operation.Run(context2);
        }
    }
}
