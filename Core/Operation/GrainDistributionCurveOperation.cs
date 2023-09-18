using Core.Interfaces.Operation;
using Core.Operation.InternalOutputs;
using Core.Operation.OperationTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class GrainDistributionCurveOperation:PoreSizeStatOperationBase
    {
        public override string Name => nameof(GrainDistributionCurveOperation);
        public override OperationProperties DefaultOperationProperties =>
            new GrainDistributionCurveOperationProperties() { AdditiveType = GrainDistributionCurveOperationProperties.AdditiveTypeEnum.GRAVEL };

        private double Density = Math.Pow(10, -6); //kg/m3 --> g/mm3
        private double Dmax = 0;
        private string internalOutputName = "GrainDistributionCurve";

        public override async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            Dmax = 0;
            var properties = context.OperationProperties as GrainDistributionCurveOperationProperties;
            Density *= (int)properties.AdditiveType;
            ValueAdder = (key, pore) => Sieves[key] += pore.Volume * Density;

            //Ha be akarjuk allitani az algoritmust akkor azt még a base run hivas elott kell. kulonben simpleAlgorithm-mel fut
            context = await base.Run(context, progress, token);

            if (token.IsCancellationRequested || GrainSizesAndVolumes.Count==0)
                return context;


            double distribution = 0;
            double sumWeight = Sieves.Sum(x => x.Value);
            for (int i = 0; i < Sieves.Count; i++)
            {
                distribution = distribution + Sieves.ElementAt(i).Value;
                double percentage = Math.Round(((distribution / sumWeight) * 100), 2);

                if (percentage >= 95.00 && Dmax == 0)
                    Dmax = Sieves.ElementAt(i).Key;

                Percentages.Add(percentage);
            }

            var outputName = properties.HistogramName == "" ? "Grain Ditribution Curve" : properties.HistogramName;
            internalOutputName = properties.HistogramName == "" ? internalOutputName : properties.HistogramName.Replace(" ", "");

            var distributionModel = PoreDistributionInternalOutput.CreateDistribution
                (outputName + "(Dmax:" + Dmax + "mm)", "Sieze Size", "Pores/Area Ratio",
                Sieves.Keys.ToArray<double>(), Percentages.ToArray(), Dmax);

            context.AddInternalOutput(internalOutputName, distributionModel);
            return context;
        }
    }

    public class GrainDistributionCurveOperationProperties : OperationProperties
    {
        public AdditiveTypeEnum AdditiveType { get; set; }
        public String HistogramName { get; set; } = "Grain Distribution Curve";
        public enum AdditiveTypeEnum //kg/m3
        {
            GRAVEL = 2645,
            DOLOMITE = 2850,
            LIME_STONE = 2710,
            ANDESITE = 2700,
            BASALT = 3000,
        }
    }
}
