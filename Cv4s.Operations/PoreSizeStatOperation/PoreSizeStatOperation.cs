using Cv4s.Common.Exceptions;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Operations.OperationTools;
using Cv4s.Operations.PoreSizeStatOperation.Models;

namespace Cv4s.Operations.PoreSizeStatOperation
{
    public class PoreSizeStatOperation : PoreSizeStatOperationBase
    {
        private readonly IUIHandlerService _uIHandlerService;

        private int allGrainCount = 0;
        private double limitSizeForHistogram = 0.25;

        public PoreSizeStatOperation(IUIHandlerService uIHandlerService)
        {
            _uIHandlerService = uIHandlerService;

            Properties = new PoreSizeStatOperationProperties();

            RunEventArgs.IsCallableFromButton = true;
            RunEventArgs.IsCallableFromCanvas = false;
        }


        public void CalcPoreSizeStat(IRawImageSource rawImages, IBlobImageSource blobImages)
        {
            //preset and reset items for the calculation
            Sieves.Clear();
            GrainSizesAndVolumes.Clear();
            Percentages.Clear();
            allBlobIds.Clear();

            _uIHandlerService.ShowMeasurementEditor(this, rawImages, blobImages);

            var properties = Properties as PoreSizeStatOperationProperties;
            IPoreSizeAlgorithm? algorithm = null;

            switch (properties!.Algorithm)
            {
                case Models.AlgorithmOption.Simple:
                    algorithm = new SimpleAlgorithm();
                    break;
                case Models.AlgorithmOption.Refined:
                    algorithm = new RefinedAlgorithm();
                    break;
                case Models.AlgorithmOption.RotatedRectangle:
                    algorithm = new RotatedRectangleAlgorithm();
                    break;
                default:
                    throw new ArgumentNullException("Not implemented pore size calculation algoritm!");
            }

            SievesValueAdder valueAdder = (key, pore) => Sieves[key] += 1;

            //TODO you can define the algorithm for the size determination
            GrainSizesAndVolumes = CalculateDistribution(rawImages,blobImages,valueAdder,algorithm);

            allGrainCount = GrainSizesAndVolumes.Count();

            if (allGrainCount == 0)
                throw new BusinessException("No grain/pore found!!");

            double grainCount = 0;
            double percentage = 0;

            limitSizeForHistogram = properties.MinSieveSize;

            foreach (var item in Sieves)
            {
                grainCount = item.Value;
                percentage = Math.Round(((grainCount / allGrainCount) * 100), 2);

                if (item.Key <= limitSizeForHistogram)
                {
                    if (Percentages.Count == 0)
                        Percentages.Add(percentage);
                    else
                        Percentages[0] += percentage;
                }
                else
                {
                    Percentages.Add(percentage);
                }
            }

            //add internal output

            var maxSieveSize = Sieves.Keys.Max();

            var SizeHistogram = PlotHelper.CreateColumnPlot
                (properties.HistogramName == "" ? "Pore Histogram" : properties.HistogramName, "Sieze Size", "Pores/Area Ratio",
                Sieves.Keys.Where(x => x >= limitSizeForHistogram).ToArray<double>(), Percentages.ToArray());

            _uIHandlerService.ShowOxyPlotViewer(SizeHistogram);
        }
    }
}
