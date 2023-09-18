using Core.Enums;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation.OperationTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class ComponentAreaRatioOperation : IOperation
    {
        public string Name => "ComponentAreaRatioOperation";

        public static string GetAreaRatioHistogramName(ImagePlane.Direction direction, string material)
            => $"AreaRatioHistogram{material}{direction}";

        public static string GetAreaRatioValuesOutputmName(ImagePlane.Direction direction, string material)
            => $"AreaRatioValues{material}{direction}";

        public OperationProperties DefaultOperationProperties => new ComponentAreaRatioOperationProperties();

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {

            var properties = context.OperationProperties as ComponentAreaRatioOperationProperties;
            var direction = properties.Direction;
            var material = properties.Material.ToString();
            var images = context.BlobImages;

            int length = 0;
            switch (direction)
            {
                case ImagePlane.Direction.X:
                    length = images[0].Size.Width;
                    break;
                case ImagePlane.Direction.Y:
                    length = images[0].Size.Height;
                    break;
                case ImagePlane.Direction.Z:
                    length = images.Count;
                    break;
            }

            var oxyplotRatioValues = new int[length];
            var layerIndices = new int[length];
            var output = new ComponentsAreaRatioInternalOutput()
            {
                RatioValues = new double[length]
            };

            for (int i = 0; i < length; i++)
            {
                var plane = new ImagePlane(images, direction, i);
                double ratio = CalculateAreaRatio(plane, material);
                oxyplotRatioValues[i] = (int)Math.Round(ratio * 100);
                output.RatioValues[i] = ratio;
                layerIndices[i] = i + 1;
                progress.Report((i + 1) / (double)length);
            }

            var oxyplot = OxyplotInternalOutput.CreateScatterPlot(
                $"Component ({material}) area ratios in {direction} direction", "Layer", "Area ratio(%)",
                layerIndices, oxyplotRatioValues);

            context.AddInternalOutput(GetAreaRatioValuesOutputmName(direction, material), output);
            context.AddInternalOutput(GetAreaRatioHistogramName(direction, material), oxyplot);

            return context;
        }

        private double CalculateAreaRatio(ImagePlane plane, string materailTagName)
        {
            int totalCount = 0;
            int componentCount = 0;
            for (int i = 0; i < plane.Width; i++)
            {
                for (int j = 0; j < plane.Heigth; j++)
                {
                    int blobId = plane.GetBlobIdAt(i, j);
                    if (blobId != 0)
                    {
                        var tags = plane.GetTagsAt(i, j);
                        totalCount++;
                        if (tags.Any(t => t.Name == materailTagName && t.Value == 0))
                        {
                            componentCount++;
                        }
                    }
                }
            }
            if (totalCount == 0)  return 0;

            return componentCount / (double)totalCount;
        }
    }

    public class ComponentsAreaRatioInternalOutput : InternalOutput
    {
        public double[] RatioValues { get; set; }
    }

    public class ComponentAreaRatioOperationProperties : OperationProperties
    {
        public ImagePlane.Direction Direction { get; set; } = ImagePlane.Direction.Z;

        public MaterialTags Material { get; set; }
    }
}
