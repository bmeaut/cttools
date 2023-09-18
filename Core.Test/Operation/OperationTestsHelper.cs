using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Test.Operation
{
    class OperationTestsHelper
    {
        public static OperationContext CreateOperationContext(IOperation op)
        {
            const int NumberOfLayers = 5;
            var context = new OperationContext();
            context.BlobImages = new Dictionary<int, IBlobImage>();
            for (int i = 0; i < NumberOfLayers; i++)
            {
                // for the tests we do not use BlobImageProxy instances.
                context.BlobImages.Add(i, new BlobImage(200, 200));
            }
            context.ActiveLayer = 0;
            context.OperationProperties = op.DefaultOperationProperties;
            context.OperationRunEventArgs = new Services.OperationRunEventArgs();
            return context;
        }

    }
}
