using Core.Enums;
using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation.OperationTools;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Core.Operation {
    /// <summary>
    /// Dummy operation to test basic interfaces.
    /// Sets upper half of the image to blobID 1 and adds the tag
    ///     "dummy",42 to the new blob.
    /// </summary>
    public class AggregateRatioOperation : IOperation {
        public string Name => "AggregateRatioOperation";

        public OperationProperties DefaultOperationProperties => new AggregateRatioOperationProperties();


        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token) {
            var blobImages = context.BlobImages;

            var numOfEnums = 0;
            foreach (var MaterialTag in (MaterialTags[])Enum.GetValues(typeof(MaterialTags))) {
                numOfEnums++;
            }

            int[] materialCountList = new int[numOfEnums];

            foreach (var blobImagePair in blobImages) {
                var blobImage = blobImagePair.Value;

                for (int y = 0; y < blobImage.Size.Height; y++) {
                    for (int x = 0; x < blobImage.Size.Width; x++) {
                        var id = blobImage[y, x];
                        List<Tag> materialValue;
                        blobImage.Tags.TryGetValue(id, out materialValue);

                        if (materialValue == null) { continue; }
                        var materialTag = materialValue.First();    //nullpointer exception lehet

                        materialCountList[materialTag.Value]++;
                    }
                }
            }

            int[] indices = new int[materialCountList.Length];
            for (int i = 0; i < materialCountList.Length; i++) {
                indices[i] = i;
            }

            var output = OxyplotInternalOutput.CreateScatterPlot(
                "Aggregate Ratio", "Aggregate type", "Ratio",
                indices,
                materialCountList
                );

            Action action = delegate () {
                output.ExportToPngFile("ratio_export.png");
            };
            await Application.Current.Dispatcher.BeginInvoke(action);

            return context;
        }
    }

    public class AggregateRatioOperationProperties : OperationProperties {

    }
}
