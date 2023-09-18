using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation.OperationTools;
using OpenCvSharp;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    /// <summary>
    /// Dummy operation to test basic interfaces.
    /// Sets upper half of the image to blobID 1 and adds the tag
    ///     "dummy",42 to the new blob.
    /// </summary>
    public class StatisticsOperation : IOperation
    {
        public string Name => "StatisticsOperation";

        public OperationProperties DefaultOperationProperties => new StatisticsOperationProperties();


        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var blobImages = context.BlobImages;
            var nonBlobCounter = 0;
            var blobCounter = 0;


            foreach (var blobImagePair in blobImages)
            {
                var blobImage = blobImagePair.Value;
                var numOfBlobs = blobImage.Tags.Count;
                var indexOfConcrete = blobImage.Tags.Aggregate((l, r) => l.Key > r.Key ? l : r).Key;

                for (int x = 0; x < blobImage.Size.Width; x++)
                {
                    for (int y = 0; y < blobImage.Size.Height; y++)
                    {
                        //Hogyan lehet meghatározni a cement blobjának (és a cementen kívül eső rész) id-jét? 
                        if (blobImage[y, x] == 1) { }
                        else if (blobImage[y, x] == indexOfConcrete) { nonBlobCounter++; }
                        else { blobCounter++; }
                    }
                }
            }

            var ratio = (double)blobCounter / (blobCounter + nonBlobCounter);

            Console.WriteLine(ratio);

            return context;
        }
    }

    public class StatisticsOperationProperties : OperationProperties
    {

    }
}
