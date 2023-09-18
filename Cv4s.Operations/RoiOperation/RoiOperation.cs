using Cv4s.Common.Enums;
using Cv4s.Common.Extensions;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Operations.OperationTools;
using OpenCvSharp;

namespace Cv4s.Operations.RoiOperation
{
    public class RoiOperation : OperationBase
    {
        private readonly IUIHandlerService _uIHandlerService;
        private bool _continueOperation = true;

        public RoiOperation(IUIHandlerService uIHandlerService)
        {
            _uIHandlerService = uIHandlerService;

            Properties = new RoiOperationProperties();

            this.RunEventArgs.IsCallableFromButton = false;
            this.RunEventArgs.IsCallableFromCanvas = true;
        }

        public IBlobImageSource CalcRoi(IRawImageSource rawImages, IBlobImageSource blobImages)
        {
            while (_continueOperation)
            {
                _uIHandlerService.ShowMeasurementEditor(this, rawImages, blobImages);

                var props = Properties as RoiOperationProperties;

                if (RunEventArgs.CollectedStrokes == null || props == null)
                    throw new ArgumentNullException("Missing Collected Strokes parameter!");

                _continueOperation = props.ContinueOperation;

                switch (props.Mode)
                {
                    case ModeEnum.MarkBlobsToRemove:
                        if (RunEventArgs.SelectedLayer == null)
                            throw new ArgumentNullException("Missing SelectedLayer parameter!");

                        MarkSelectedBlobs(RunEventArgs.CollectedStrokes, blobImages, RunEventArgs.SelectedLayer!.Value);
                        break;
                    case ModeEnum.MarkAreaToRemove:
                        if (RunEventArgs.SelectedLayer == null)
                            throw new ArgumentNullException("Missing SelectedLayer parameter!");

                        MarkSelectedArea(blobImages[RunEventArgs.SelectedLayer!.Value], RunEventArgs.CollectedStrokes);
                        break;
                    case ModeEnum.MarkAreaToRemoveOnAllLayers:
                        MarkAreaOnAllLayers(blobImages, RunEventArgs.CollectedStrokes);
                        break;
                    case ModeEnum.UnmarkArea:
                        if (RunEventArgs.SelectedLayer == null)
                            throw new ArgumentNullException("Missing SelectedLayer parameter!");

                        UnmarkSelectedArea(blobImages, RunEventArgs.SelectedLayer!.Value, RunEventArgs.CollectedStrokes);
                        break;
                    case ModeEnum.AutoMarkOuterBlobsToRemove:
                        AutoMarkOuterBlobs(blobImages);
                        break;
                    case ModeEnum.MarkLayerToRemove:
                        if (RunEventArgs.SelectedLayer == null)
                            throw new ArgumentNullException("Missing SelectedLayer parameter!");

                        MarkLayerToRemove(blobImages, RunEventArgs.SelectedLayer!.Value);
                        break;
                    case ModeEnum.RemoveMarkedAreasFromRoi:
                        RemoveMarkedAreas(blobImages);
                        break;
                }
            }

            return blobImages;

        }

        private void MarkSelectedBlobs(Point[][] strokes, IBlobImageSource blobImages, int layer)
        {
            var blobImage = blobImages[layer];
            var blobIds = blobImage.GetHitBlobIds(strokes);
            blobImage.SetTags(blobIds, nameof(Tags.RemoveFromRoiTag)); //Lekezeljük a nulla értéket az extension methodban
        }

        private void AutoMarkOuterBlobs(IBlobImageSource blobImages)
        {
            foreach (var layer in blobImages.Keys)
            {
                var blobImage = blobImages[layer];
                int topLeftBlobId = blobImage[0, 0];
                int topRightBlobId = blobImage[0, blobImage.Size.Width - 1];
                if (topLeftBlobId == topRightBlobId)
                {
                    var outerBlobId = topLeftBlobId;
                    if (outerBlobId != 0)
                        blobImage.SetTag(outerBlobId, nameof(Tags.RemoveFromRoiTag));
                }
                else
                {
                    Console.WriteLine($"Couldn't find blob to remove at layer{layer}");
                }
            }
        }

        private void MarkLayerToRemove(IBlobImageSource blobImages, int layer)
        {
            var blobimage = blobImages[layer];
            var blobids = blobimage.CollectAllRealBlobIds();
            blobimage.SetTags(blobids, nameof(Tags.RemoveFromRoiTag));
        }

        private void MarkAreaOnAllLayers(IBlobImageSource blobImages, Point[][] strokes)
        {
            Parallel.ForEach(blobImages.Keys, layer =>
            {
                var blobImage = blobImages[layer];
                MarkSelectedArea(blobImage, strokes);
            });
        }

        private void MarkSelectedArea(IBlobImage blobImage, Point[][] strokes)
        {
            var selectedAreaMask = StrokeHelper.GetMaskForStrokes(blobImage, strokes, true);

            var affectedBlobs = blobImage.GetBlobIdsHitByMask(selectedAreaMask);
            var unmarkedAffectedBlobsByComponent = SeparateUnmarkedBlobsByComponent(blobImage, affectedBlobs);
            var materialTagsByComponent = GetMaterialTagsByComponent(blobImage, unmarkedAffectedBlobsByComponent);

            foreach (var componentId in unmarkedAffectedBlobsByComponent.Keys)
            {
                var unmarkedAffectedBlobs = unmarkedAffectedBlobsByComponent[componentId];
                var newBlobs = CutBlobsWithMask(blobImage, selectedAreaMask, unmarkedAffectedBlobs);
                foreach (var blob in newBlobs)
                {
                    blobImage.SetTag(blob, Tags.ComponentId.ToString(), componentId);
                    if (materialTagsByComponent.ContainsKey(componentId))
                    {
                        blobImage.SetTag(blob, materialTagsByComponent[componentId]);
                    }
                    blobImage.SetTag(blob, nameof(Tags.RemoveFromRoiTag));
                }
            }
        }

        private void UnmarkSelectedArea(IBlobImageSource blobImages, int layer, Point[][] strokes)
        {
            var blobImage = blobImages[layer];
            var selectedAreaMask = StrokeHelper.GetMaskForStrokes(blobImage, strokes, true);

            var affectedBlobs = blobImage.GetBlobIdsHitByMask(selectedAreaMask);
            var markedAffectedBlobsByComponent = SeparateMarkedBlobsByComponent(blobImage, affectedBlobs);
            var materialTagsByComponent = GetMaterialTagsByComponent(blobImage, markedAffectedBlobsByComponent);

            foreach (var componentId in markedAffectedBlobsByComponent.Keys)
            {
                var markedAffectedBlobs = markedAffectedBlobsByComponent[componentId];
                var newBlobs = CutBlobsWithMask(blobImage, selectedAreaMask, markedAffectedBlobs);
                foreach (var blob in newBlobs)
                {
                    blobImage.SetTag(blob, Tags.ComponentId.ToString(), componentId);
                    if (materialTagsByComponent.ContainsKey(componentId))
                    {
                        blobImage.SetTag(blob, materialTagsByComponent[componentId]);
                    }
                }
            }
        }

        private IEnumerable<int> CutBlobsWithMask(IBlobImage blobimage, Mat mask, List<int> blobsToSeparate)
        {
            var newToOldBlobIdsMapper = new Dictionary<int, int>();
            var nextId = blobimage.GetNextUnusedBlobId();
            foreach (var blob in blobsToSeparate)
            {
                newToOldBlobIdsMapper.Add(blob, nextId);
                nextId++;
            }

            var maskIndexer = mask.GetGenericIndexer<byte>();
            for (int x = 0; x < blobimage.Size.Width; x++)
            {
                for (int y = 0; y < blobimage.Size.Height; y++)
                {
                    var blobId = blobimage[y, x];
                    if (maskIndexer[y, x] != 0 && newToOldBlobIdsMapper.ContainsKey(blobId))
                    {
                        blobimage[y, x] = newToOldBlobIdsMapper[blobId];
                    }
                }
            }

            var newBlobIds = newToOldBlobIdsMapper.Values;
            return newBlobIds;
        }

        private Dictionary<int, List<int>> SeparateBlobsByComponent(
            IBlobImage blobImage,
            IEnumerable<int> blobsToSeparate,
            bool marked)
        {
            var blobsByComponent = new Dictionary<int, List<int>>();
            foreach (var blob in blobsToSeparate)
            {
                bool isMarked = (blobImage.GetTagValueOrNull(blob, nameof(Tags.RemoveFromRoiTag)) != null);
                if (marked != isMarked)
                {
                    continue;
                }
                int componentId = (int)blobImage.GetTagValueOrNull(blob, Tags.ComponentId.ToString());
                if (!blobsByComponent.ContainsKey(componentId))
                {
                    blobsByComponent.Add(componentId, new List<int>());
                }
                blobsByComponent[componentId].Add(blob);
            }
            return blobsByComponent;
        }

        private Dictionary<int, List<int>> SeparateMarkedBlobsByComponent(IBlobImage blobImage, IEnumerable<int> blobsToSeparate)
             => SeparateBlobsByComponent(blobImage, blobsToSeparate, marked: true);

        private Dictionary<int, List<int>> SeparateUnmarkedBlobsByComponent(IBlobImage blobImage, IEnumerable<int> blobsToSeparate)
            => SeparateBlobsByComponent(blobImage, blobsToSeparate, marked: false);

        private Dictionary<int, List<int>> SeparateAllBlobsByComponent(IBlobImage blobImage)
            => SeparateBlobsByComponent(blobImage, blobImage.CollectAllRealBlobIds(), marked: false);

        private Mat[] GetUnionMasksByComponent(IBlobImage blobImage, Dictionary<int, List<int>> blobsByComponent)
        {
            var result = new Mat[blobsByComponent.Keys.Count];
            foreach (var componentId in blobsByComponent.Keys)
            {
                result[componentId] = blobImage.GetMaskUnion(blobsByComponent[componentId]);
            }
            return result;
        }

        private Dictionary<int, string> GetMaterialTagsByComponent(IBlobImage blobImage, Dictionary<int, List<int>> blobsByComponent)
        {
            var materialTagsByComponent = new Dictionary<int, string>();
            foreach (var componentId in blobsByComponent.Keys)
            {
                var blob = blobsByComponent[componentId].First();
                var tagNames = blobImage.GetTagsForBlob(blob).Select(b => b.Name);
                var materialTagNames = Enum.GetNames(typeof(MaterialTag)).ToList();
                var intersection = tagNames.Intersect(materialTagNames);
                if (intersection.Count() == 1)
                {
                    var materialTag = intersection.First();
                    materialTagsByComponent.Add(componentId, materialTag);
                }
            }
            return materialTagsByComponent;
        }

        private void FixSegmentation(IBlobImage blobImage)
        {
            var blobsByComponent = SeparateAllBlobsByComponent(blobImage);
            var unionMasksByComponent = GetUnionMasksByComponent(blobImage, blobsByComponent);
            var materialTagsByComponent = GetMaterialTagsByComponent(blobImage, blobsByComponent);

            for (int componentId = 0; componentId < unionMasksByComponent.Length; componentId++)
            {
                var blobs = ConnectedComponentsHelper.SegmentMask(blobImage, unionMasksByComponent[componentId]);
                foreach (var blob in blobs)
                {
                    blobImage.SetTag(blob, Tags.ComponentId.ToString(), componentId);
                    if (materialTagsByComponent.ContainsKey(componentId))
                    {
                        blobImage.SetTag(blob, materialTagsByComponent[componentId]);
                    }
                }
            }
        }

        private void RemoveMarkedAreas(IBlobImageSource blobImages)
        {
            var blobs = blobImages.ToDictionary().Values;

            Parallel.ForEach(blobs, blobImage =>
            {
                var blobIdsToRemove = blobImage.GetBlobsByTagValue(nameof(Tags.RemoveFromRoiTag), 0).ToList();
                blobImage.RemoveTags(blobIdsToRemove, nameof(Tags.RemoveFromRoiTag));
                var unionMask = blobImage.GetMaskUnion(blobIdsToRemove);
                blobImage.SetBlobMask(unionMask, 0);
                // In extremely rare cases, segmentation might be reqired
                // with only 1 blob to remove, 
                if (blobIdsToRemove.Count > 1)
                    FixSegmentation(blobImage);
            });
        }
    }
}
