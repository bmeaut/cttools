using Core.Enums;
using Core.Interfaces.Operation;
using Core.Operation.OperationTools;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class ComponentCountOperation : IOperation
    {
        public string Name => "ComponentCountOperation";

        public static string GetComponentCountHistogramName(ImagePlane.Direction direction, string material)
            => $"ComponentCountHistogram{material}{direction}";

        public static string GetComponentCountOutputmName(ImagePlane.Direction direction, string material)
            => $"ComponentCount{material}{direction}";

        public OperationProperties DefaultOperationProperties => new ComponentCountOperationProperties();

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {

            var properties = context.OperationProperties as ComponentCountOperationProperties;
            var direction = properties.Direction;
            var material = properties.Material.ToString(); ;
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

            var layerIndices = new int[length];
            var output = new ComponentsAreaRatio()
            {
                ComponentCounts = new int[length]
            };

            for (int i = 0; i < length; i++)
            {
                var plane = new ImagePlane(images, direction, i);
                output.ComponentCounts[i] = CalculateComponentCount(plane, material);
                layerIndices[i] = i + 1;
                progress.Report((i + 1) / (double)length);
            }

            var oxyplot = OxyplotInternalOutput.CreateScatterPlot(
                $"Connected components({material}) in {direction} direction", "Layer", "Count",
                layerIndices, output.ComponentCounts);

            context.AddInternalOutput(GetComponentCountOutputmName(direction, material), output);
            context.AddInternalOutput(GetComponentCountHistogramName(direction, material), oxyplot);

            return context;
        }

        private int CalculateComponentCount(ImagePlane plane, string material)
        {
            var steelFiberMask = new Mat(plane.Heigth, plane.Width, MatType.CV_8UC1, 0);
            var steelFiberMaskIndexer = steelFiberMask.GetGenericIndexer<byte>();

            for (int i = 0; i < plane.Width; i++)
            {
                for (int j = 0; j < plane.Heigth; j++)
                {
                    var tags = plane.GetTagsAt(i, j);
                    if (tags.Any(t => t.Name == material))
                    {
                        steelFiberMaskIndexer[j, i] = 255;
                    }
                }
            }

            var unusedLabeledImage = new Mat(steelFiberMask.Size(), MatType.CV_32SC1);
            return Cv2.ConnectedComponents(steelFiberMask, unusedLabeledImage, PixelConnectivity.Connectivity4);
        }

        public class ComponentsAreaRatio : InternalOutput
        {
            public int[] ComponentCounts { get; set; }
        }
    }
    public class ComponentCountOperationProperties : OperationProperties
    {
        public ImagePlane.Direction Direction { get; set; } = ImagePlane.Direction.Z;

        public MaterialTags Material { get; set; }
    }
}
