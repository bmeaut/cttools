using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation.OperationTools;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class ManualEditOperation : IOperation
    {
        public string Name => "Manual edit";

        public OperationProperties DefaultOperationProperties => new ManualEditOperationProperties();

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var props = context.OperationProperties as ManualEditOperationProperties;
            var strokes = context.OperationRunEventArgs.Strokes;
            IBlobImage blobImage = context.BlobImages[context.ActiveLayer];

            if (strokes != null)
            {
                switch (props.Mode)
                {
                    case ManualEditOperationProperties.ModeEnum.Slice:
                        StrokeHelper.Slice(blobImage, strokes);
                        break;
                    case ManualEditOperationProperties.ModeEnum.Extend:
                        StrokeHelper.Extend(blobImage, props.IsStrokeClosed, strokes);
                        break;
                    case ManualEditOperationProperties.ModeEnum.AddNew:
                        StrokeHelper.Add(blobImage, blobImage.GetNextUnusedBlobId(), props.IsStrokeClosed, strokes);
                        break;
                    case ManualEditOperationProperties.ModeEnum.Subtract:
                        StrokeHelper.Subtract(blobImage, props.IsStrokeClosed, strokes);
                        break;
                    case ManualEditOperationProperties.ModeEnum.Select:
                        Select(blobImage, props, strokes);
                        break;
                    case ManualEditOperationProperties.ModeEnum.Delete:
                        StrokeHelper.Delete(blobImage, strokes);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown Mode selected for ManualEditOperation: {props.Mode}");
                }
            }

            return context;
        }
        private void Select(IBlobImage blobImage, ManualEditOperationProperties props, Point[][] strokes)
        {
            if (props.TagName == null || props.TagName == string.Empty)
                return;

            var blobIds = StrokeHelper.GetBlobIdsHitByStrokePoints(blobImage, strokes);
            foreach (int blobId in blobIds)
                if (blobId != 0)
                    blobImage.SetTag(blobId, props.TagName);
        }
    }

    public class ManualEditOperationProperties : OperationProperties
    {
        public ModeEnum Mode { get; set; } = ModeEnum.AddNew;
        public bool IsStrokeClosed { get; set; } = true;
        public string TagName { get; set; } = "selected";

        public enum ModeEnum
        {
            Slice,
            Extend,
            AddNew,
            Select,
            Subtract,
            Delete
        }
    }
}
