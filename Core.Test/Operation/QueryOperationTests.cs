using Core.Image;
using Core.Interfaces.Image;
using Core.Operation;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace Core.Test.Operation
{
    public class QueryOperationTests
    {
        private readonly QueryOperation op = new QueryOperation();
        private readonly OperationContext context;
        private readonly CancellationToken token = new CancellationToken();

        private QueryOperation.QueryOperationProperties props =>
            context.OperationProperties as QueryOperation.QueryOperationProperties;

        const string testTag1Name = "testTag1";
        const int testTag1Value = 3;
        const string testTag2Name = "testTag2";
        const int testTag2Value = 42;

        private readonly Point pointInsideBlob1 = new Point(1, 1);
        private readonly Point pointInsideBlob2 = new Point(51, 1);

        public QueryOperationTests()
        {
            context = OperationTestsHelper.CreateOperationContext(op);
            var blobImage0 = context.BlobImages[0];
            blobImage0.DrawBlobRect(0, 0, 40, 100, blobId: 1);
            blobImage0.SetTag(1, testTag1Name, testTag1Value);

            blobImage0.DrawBlobRect(50, 0, 50, 100, blobId: 2);
            blobImage0.SetTag(2, testTag2Name, testTag2Value);
        }

        [Fact]
        public async void GetTagsAndValuesForBlobs()
        {
            List<string> infoStrings = new List<string>();
            op.ActionToOutputInformationRow =
                (string info) => infoStrings.Add(info);

            // Stroke on blob 1
            context.OperationRunEventArgs.Strokes =
                new Point[][] { new Point[] {
                    pointInsideBlob1, pointInsideBlob2 } };

            await op.Run(context, new Progress<double>(), token);

            // Now query results should appear in some output. Log maybe?
            Assert.NotEmpty(infoStrings);
            Assert.Contains($"{testTag1Name}={testTag1Value}", infoStrings);
            Assert.Contains($"{testTag2Name}={testTag2Value}", infoStrings);
        }
    }
}
