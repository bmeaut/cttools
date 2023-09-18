using Core.Interfaces.Operation;
using Core.Operation.InternalOutputs;
using Core.Operation.OperationTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace Core.Operation
{
    public class PoreSizeStatOperation : PoreSizeStatOperationBase
    {
        private int allGrainCount = 0;
        private string internalOutputName = "PoreSizeHistogram";
        public override OperationProperties DefaultOperationProperties => new PoreSizeStatOperationProperties();
        public override string Name => nameof(PoreSizeStatOperation);
        private double limitSizeForHistogram = 0.25;

        /// <summary>
        /// Calls the <see cref="PoreSizeStatOperationBase"/> run method, for the sizes of the pores/grains
        /// After that it collects all the pores/grains into the sieves, depending on the pore/Grain sizes
        /// Calculates the sieveCount/allCount percentages.
        /// Creates the <see cref="PoreSizeInternalOutput"/> which will be a ColumnPlot
        /// </summary>
        /// <param name="context"></param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public override async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            Sieves.Clear();
            GrainSizesAndVolumes.Clear();
            Percentages.Clear();
            allBlobIds.Clear();

            var properties = context.OperationProperties as PoreSizeStatOperationProperties;
            ValueAdder = (key, pore) => Sieves[key] += 1;

            //TODO you can define the algorithm for the size determination
            context = await base.Run(context, progress, token);
            allGrainCount = GrainSizesAndVolumes.Count();
            
            if(allGrainCount == 0)
                return context;

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

            var outputName = properties.HistogramName == "" ? internalOutputName : properties.HistogramName;
            internalOutputName = properties.HistogramName == "" ? internalOutputName : properties.HistogramName.Replace(" ", "");

            var SizeHistogram = PoreSizeInternalOutput.CreateColumnPlot
                (outputName, "Sieze Size", "Pores/Area Ratio",
                Sieves.Keys.Where(x=>x>=limitSizeForHistogram).ToArray<double>(),Percentages.ToArray());

            context.AddInternalOutput(internalOutputName, SizeHistogram);

            return context;
        }


        public class PoreSizeStatOperationProperties : OperationProperties
        {
            public String HistogramName { get; set; } = "PoreSizeHistogram";
            public double MinSieveSize { get; set; } = 0.25;
           // public Color HistogramColor { get; set; } = Color.Red;
        }

    }
}
