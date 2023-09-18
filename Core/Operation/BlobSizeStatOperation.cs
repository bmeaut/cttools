using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class BlobSizeStatOperation : IOperation
    {
        public string Name => nameof(BlobSizeStatOperation);

        public OperationProperties DefaultOperationProperties => new BlobSizeStatOperationProperties();

        public static readonly string OxyplotOutputName = "BlobSizeHistogram";

        public static readonly string AreaInPixelTagName = "AreaInPixel";
        public static readonly string AreaInUm2TagName = "AreaInUm2";

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var stat = new BlobSizeStat();

            // TODO: do it for all image layers, enumerate blob images
            var blobImage = context.ActiveBlobImage;
            var blobIds = blobImage.CollectAllRealBlobIds();

            int sumArea = 0;
            var blobAreaDictionary = GetBlobId2AreaDictionary(blobImage);
            var areaDictionary = new Dictionary<int, int>();
            var squareum2PerPixel = context.RawImageMetadata.XResolution
                * context.RawImageMetadata.XResolution * 1e6;
            foreach (var blobId in blobIds)
            {
                var area = blobAreaDictionary[blobId];
                var areaInUm2 = area * squareum2PerPixel;
                blobImage.SetTag(blobId, AreaInPixelTagName, area);
                blobImage.SetTag(blobId, AreaInUm2TagName,
                    (int)Math.Round(areaInUm2));
                sumArea += area;
                if (areaDictionary.ContainsKey(area))
                    areaDictionary[area]++;
                else
                    areaDictionary.Add(area, 1);
            }
            stat.SumBlobAreaInSquarePixel = sumArea;

            stat.BlobAreaHistogramAreaValues = areaDictionary.Keys
                .OrderBy(v => v).ToArray();
            stat.BlobAreaHistogramCounters =
                stat.BlobAreaHistogramAreaValues
                .Select(area => areaDictionary[area]).ToArray();
            //context.AddInternalOutput(stat);

            var cumulativeCounters = new int[stat.BlobAreaHistogramAreaValues.Length];
            int counterSumSoFar = 0;
            for (int i = 0; i < cumulativeCounters.Length; i++)
            {
                counterSumSoFar += stat.BlobAreaHistogramCounters[i];
                cumulativeCounters[i] = counterSumSoFar;
            }

            var oxyplotModel = OxyplotInternalOutput.CreateScatterPlot
                ("Blob count under area", "Area", "Not larger blob count",
                stat.BlobAreaHistogramAreaValues, cumulativeCounters);
            context.AddInternalOutput(OxyplotOutputName, oxyplotModel);

            exportDiagramToFileIfAskedFor(context, oxyplotModel);

            return context;
        }

        private Dictionary<int, int> GetBlobId2AreaDictionary(IBlobImage blobImage)
        {
            var dict = new Dictionary<int, int>();
            for (var y = 0; y < blobImage.Size.Height; y++)
                for (var x = 0; x < blobImage.Size.Width; x++)
                {
                    var blobId = blobImage[y, x];
                    if (dict.ContainsKey(blobId))
                        dict[blobId]++;
                    else
                        dict.Add(blobId, 1);
                }
            return dict;
        }

        private void exportDiagramToFileIfAskedFor(OperationContext context, OxyplotInternalOutput oxyplotModel)
        {
            var props = context.OperationProperties as BlobSizeStatOperationProperties;
            if (props.DiagramOutputFilename != null)
                oxyplotModel.ExportToPngFile(props.DiagramOutputFilename);
        }

        public class BlobSizeStat : IInternalOutput
        {
            public int SumBlobAreaInSquarePixel { get; set; }
            public int[] BlobAreaHistogramAreaValues { get; set; }
            public int[] BlobAreaHistogramCounters { get; set; }
        }
    }
    public class BlobSizeStatOperationProperties : OperationProperties
    {
        public string DiagramOutputFilename { get; set; }

        public bool ApplyToAllLayers { get; set; } = false;

        public StatisticEnum Statistic { get; set; }

        public enum StatisticEnum
        {
            CumulariveBlobCountForMaximalArea
        }
    }
}
