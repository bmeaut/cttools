using Core.Operation;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace Core.Test.Operation
{
    public class DistanceMeasurementOperationTests
    {
        private readonly CancellationToken token = new CancellationToken();

        [Fact]
        public async void DifferentXYResolutions()
        {
            var op = new DistanceMeasurementOperation();
            var context = OperationTestsHelper.CreateOperationContext(op);
            var metaData = new RawImageMetadata()
            { XResolution = 0.1, YResolution = 0.2 };
            context.RawImageMetadata = metaData;
            context.OperationRunEventArgs.Strokes = new OpenCvSharp.Point[][]
            {
                new OpenCvSharp.Point[]
                {
                    new Point(5,5), new Point(10,5), new Point(15,25)
                }
            };

            string result = "Not called";
            op.ActionToOutputInformationRow = (txt) => result = txt;
            await op.Run(context, new Progress<double>(), token);
            var deltaX = 10 * metaData.XResolution;
            var deltaY = 20 * metaData.YResolution;
            var correctDistance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            Assert.Equal($"Distance: {correctDistance}", result);
        }

    }
}
