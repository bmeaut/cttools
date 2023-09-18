using Cv4s.Common.Enums;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Operations.OperationTools;

namespace Cv4s.Operations.SelectMaterialsOperation
{
    public class SelectMaterialsOperation : OperationBase
    {
        private readonly IUIHandlerService _uIHandlerService;
        private bool _continueOperation = true;

        public SelectMaterialsOperation(IUIHandlerService uIHandlerService)
        {
            _uIHandlerService = uIHandlerService;
            RunEventArgs.IsCallableFromCanvas = true;
            RunEventArgs.IsCallableFromButton = false;

            Properties = new SelectMaterialsOperationProperties();
        }

        public IBlobImageSource SelectMaterial(IRawImageSource rawImages, IBlobImageSource blobImages)
        {
            while (_continueOperation)
            {
                _uIHandlerService.ShowMeasurementEditor(this, rawImages, blobImages);

                var props = Properties as SelectMaterialsOperationProperties;

                var strokes = RunEventArgs.CollectedStrokes;

                if (strokes == null || RunEventArgs.SelectedLayer == null)
                    throw new ArgumentNullException("No Strokes/SelectedLayer has been set!");

                var blobImage = blobImages[RunEventArgs.SelectedLayer.Value];
                var mask = blobImage.GetEmptyMask();
                var hitBlobIds = StrokeHelper.GetBlobIdsHitByStrokePoints(blobImage, strokes);
                var hitTags = new List<ITag<int>>();

                foreach (int hitBlobId in hitBlobIds)
                {
                    if (hitBlobId != 0)
                    {
                        hitTags.AddRange(blobImage.GetTagsForBlob(hitBlobId));
                        // Setting hit blobs even if there is no Tag attached to it
                        if (!props.Remove)
                            blobImage.SetTag(hitBlobId, props.MaterialTag.ToString());
                    }
                }

                if (!props.Remove)
                {
                    AddMaterialTag(blobImages, hitTags, props.MaterialTag);
                    RemoveAllOtherMatarialTags(blobImages, hitTags, props.MaterialTag);
                }
                else
                {
                    RemoveMaterialTag(blobImages, hitTags, props.MaterialTag);
                }

                _continueOperation = props!.ContinueOperation;
            }

            return blobImages;
        }


        private void AddMaterialTag(IBlobImageSource blobImages, List<ITag<int>> hitTags, MaterialTag currentTag)
        {
            Parallel.ForEach(blobImages.Values, blobImage =>
            {
                _AddRemoveMaterialTag(blobImage, hitTags, currentTag, false);
            });
        }

        private void RemoveMaterialTag(IBlobImageSource blobImages, List<ITag<int>> hitTags, MaterialTag currentTag)
        {
            Parallel.ForEach(blobImages.Values, blobImage =>
            {
                _AddRemoveMaterialTag(blobImage, hitTags, currentTag, true);
            });
        }

        private void _AddRemoveMaterialTag(IBlobImage blobImage, List<ITag<int>> hitTags, MaterialTag currentTag, bool remove = false)
        {
            foreach (var tag in hitTags)
            {
                var blobIds = blobImage.GetBlobsByTagValue(tag.Name, tag.Value);
                if (remove)
                {
                    blobImage.RemoveTags(blobIds, currentTag.ToString());
                }
                else
                {
                    blobImage.SetTags(blobIds, currentTag.ToString());
                }
            }
        }

        private void RemoveAllOtherMatarialTags(IBlobImageSource blobImages, List<ITag<int>> hitTags, MaterialTag currentTag)
        {
            Parallel.ForEach(blobImages.Values, blobImage =>
            {
                foreach (var tag in hitTags)
                {
                    var blobIds = blobImage.GetBlobsByTagValue(tag.Name, tag.Value);
                    foreach (var otherTag in Enum.GetValues(typeof(MaterialTag)).Cast<MaterialTag>())
                        if (otherTag != currentTag)
                            blobImage.RemoveTags(blobIds, otherTag.ToString());
                }
            });
        }
    }
}
