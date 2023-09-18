using Core.Enums;
using Core.Image;
using Core.Interfaces.Image;
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
    public class SelectMaterialOperation : IOperation
    {
        public string Name => "Select materials";

        int done = 0;
        double total;
        private IProgress<double> _progress;
        private CancellationToken _token;

        public OperationProperties DefaultOperationProperties =>
            new SelectMaterialOperationProperties
            {
                Tag = MaterialTags.PORE,
                Remove = false
            };

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            _progress = progress;
            _token = token;
            done = 0;

            Point[][] strokes;
            strokes = context.OperationRunEventArgs?.Strokes;
            var operationProperties = context.OperationProperties as SelectMaterialOperationProperties;

            var blobImage = context.BlobImages[context.ActiveLayer];
            var mask = blobImage.GetEmptyMask();
            var hitBlobIds = StrokeHelper.GetBlobIdsHitByStrokePoints(blobImage, strokes);
            var hitTags = new List<ITag<int>>();
            foreach (int hitBlobId in hitBlobIds)
                if (hitBlobId != 0)
                {
                    hitTags.AddRange(blobImage.GetTagsForBlob(hitBlobId));
                    // Setting hit blobs even if there is no Tag attached to it
                    if (!operationProperties.Remove)
                        blobImage.SetTag(hitBlobId, operationProperties.Tag.ToString());
                }
            if (!operationProperties.Remove)
            {
                total = 2 * context.BlobImages.Count;
                AddMaterialTag(context.BlobImages, hitTags, operationProperties.Tag);
                RemoveAllOtherMatarialTags(context.BlobImages, hitTags, operationProperties.Tag);
            }
            if (operationProperties.Remove)
            {
                total = context.BlobImages.Count;
                RemoveMaterialTag(context.BlobImages, hitTags, operationProperties.Tag);
            }
            return context;
        }

        private void AddMaterialTag(IDictionary<int, IBlobImage> blobImages, List<ITag<int>> hitTags, MaterialTags currentTag)
        {
            Parallel.ForEach(blobImages.Values, blobImage =>
            {
                if (_token.IsCancellationRequested) return;
                _AddRemoveMaterialTag(blobImage, hitTags, currentTag, false);
            });
        }

        private void RemoveMaterialTag(IDictionary<int, IBlobImage> blobImages, List<ITag<int>> hitTags, MaterialTags currentTag)
        {
            Parallel.ForEach(blobImages.Values, blobImage =>
            {
                if (_token.IsCancellationRequested) return;
                _AddRemoveMaterialTag(blobImage, hitTags, currentTag, true);
            });
        }

        private void _AddRemoveMaterialTag(IBlobImage blobImage, List<ITag<int>> hitTags, MaterialTags currentTag, bool remove = false)
        {
            foreach (var tag in hitTags)
            {
                if (_token.IsCancellationRequested) return;
                var blobIds = blobImage.GetBlobsByTagValue(tag.Name, tag.Value);
                if (remove)
                    blobImage.RemoveTag(blobIds, currentTag.ToString());
                if (!remove)
                    blobImage.SetTag(blobIds, currentTag.ToString());
            }
            _progress.Report(done++ / total);
        }

        private void RemoveAllOtherMatarialTags(IDictionary<int, IBlobImage> blobImages, List<ITag<int>> hitTags, MaterialTags currentTag)
        {
            Parallel.ForEach(blobImages.Values, blobImage =>
            {
                foreach (var tag in hitTags)
                {
                    if (_token.IsCancellationRequested) return;
                    var blobIds = blobImage.GetBlobsByTagValue(tag.Name, tag.Value);
                    foreach (var otherTag in Enum.GetValues(typeof(MaterialTags)).Cast<MaterialTags>())
                        if (otherTag != currentTag)
                            blobImage.RemoveTag(blobIds, otherTag.ToString());
                }
                _progress.Report(done++ / total);
            });
        }
    }

    public class SelectMaterialOperationProperties : OperationProperties
    {
        public MaterialTags Tag { get; set; }
        public bool Remove { get; set; }
    }
}
