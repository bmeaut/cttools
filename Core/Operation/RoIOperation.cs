using Core.Enums;
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
    public class RoIOperation : IOperation
    {
        public string Name => "RoI";

        public OperationProperties DefaultOperationProperties =>
            new RoIOperationProperties();

        public static string RemoveFromRoiTagName = "RemoveFromRoi";

        public Action<string> ActionToOutputInformationRow { get; set; }

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var props = context.OperationProperties as RoIOperationProperties;

            switch (props.Mode)
            {
                case RoIOperationProperties.ModeEnum.MarkBlobsToRemove:
                    MarkSelectedBlobs(context);
                    break;
                case RoIOperationProperties.ModeEnum.MarkAreaToRemove:
                    MarkSelectedArea(context, context.ActiveLayer);
                    break;
                case RoIOperationProperties.ModeEnum.MarkAreaToRemoveOnAllLayers:
                    MarkAreaOnAllLayers(context, progress);
                    break;
                case RoIOperationProperties.ModeEnum.UnmarkArea:
                    UnmarkSelectedArea(context);
                    break;
                case RoIOperationProperties.ModeEnum.AutoMarkOuterBlobsToRemove:
                    AutoMarkOuterBlobs(context, progress);
                    break;
                case RoIOperationProperties.ModeEnum.MarkLayerToRemove:
                    MarkLayerToRemove(context);
                    break;
                case RoIOperationProperties.ModeEnum.RemoveMarkedAreasFromRoi:
                    RemoveMarkedAreas(context, progress);
                    break;
            }

            return context;
        }

        private void MarkSelectedBlobs(OperationContext context)
        {
            if (context.OperationRunEventArgs.Strokes == null)
                return;
            var blobImage = context.BlobImages[context.ActiveLayer];
            var blobIds = blobImage.GetHitBlobIds(context.OperationRunEventArgs.Strokes);
            foreach (var blobId in blobIds)
            {
                // Lehet ezt nem itt kéne lekezelni
                if (blobId != 0)
                {
                    blobImage.SetTag(blobIds, RemoveFromRoiTagName);
                }
            }

        }

        private void AutoMarkOuterBlobs(OperationContext context, IProgress<double> progress)
        {
            int done = 0;
            foreach (var layer in context.BlobImages.Keys)
            {
                var blobImage = context.BlobImages[layer];
                int topLeftBlobId = blobImage[0, 0];
                int topRightBlobId = blobImage[0, blobImage.Size.Width - 1];
                if (topLeftBlobId == topRightBlobId)
                {
                    var outerBlobId = topLeftBlobId;
                    if (outerBlobId != 0)
                        blobImage.SetTag(outerBlobId, RemoveFromRoiTagName);
                }
                else
                {
                    ActionToOutputInformationRow($"Couldnt find blob to remove at layer{layer}");
                }

                done++;
                progress.Report(done / (double)context.BlobImages.Count);
            }
        }

        private void MarkLayerToRemove(OperationContext context)
        {
            var blobimage = context.ActiveBlobImage;
            var blobids = blobimage.CollectAllRealBlobIds();
            foreach (var id in blobids)
            {
                blobimage.SetTag(id, RemoveFromRoiTagName);
            }
        }

        private void MarkAreaOnAllLayers(OperationContext context, IProgress<double> progress)
        {
            if (context.OperationRunEventArgs.Strokes == null)
                return;
            int done = 0;
            Parallel.ForEach(context.BlobImages.Keys, layer =>
            {
                var blobImage = context.BlobImages[layer];
                MarkSelectedArea(context, layer);
                done++;
                progress.Report(done / (double)context.BlobImages.Count);
            });
        }

        private void MarkSelectedArea(OperationContext context, int layer)
        {
            if (context.OperationRunEventArgs.Strokes == null)
                return;
            var blobImage = context.BlobImages[layer];
            var selectedAreaMask = StrokeHelper.GetMaskForStrokes(blobImage, context.OperationRunEventArgs.Strokes, true);

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
                    blobImage.SetTag(blob, RemoveFromRoiTagName);
                }
            }
        }

        private void UnmarkSelectedArea(OperationContext context)
        {
            if (context.OperationRunEventArgs.Strokes == null)
                return;
            var blobImage = context.BlobImages[context.ActiveLayer];
            var selectedAreaMask = StrokeHelper.GetMaskForStrokes(blobImage, context.OperationRunEventArgs.Strokes, true);

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
                bool isMarked = (blobImage.GetTagValueOrNull(blob, RemoveFromRoiTagName) != null);
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
                var materialTagNames = Enum.GetNames(typeof(MaterialTags)).ToList();
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

        private void RemoveMarkedAreas(OperationContext context, IProgress<double> progress)
        {
            int done = 0;
            Parallel.ForEach(context.BlobImages.Values, blobImage =>
            {
                var blobIdsToRemove = blobImage.GetBlobsByTagValue(RemoveFromRoiTagName, 0).ToList();
                blobImage.RemoveTag(blobIdsToRemove, RemoveFromRoiTagName);
                var unionMask = blobImage.GetMaskUnion(blobIdsToRemove);
                blobImage.SetBlobMask(unionMask, 0);
                // In extremely rare cases, segmentation might be reqired
                // with only 1 blob to remove, 
                if (blobIdsToRemove.Count > 1)
                    FixSegmentation(blobImage);
                progress.Report(done++ / (double)context.BlobImages.Count);
            });
        }
    }
    public class RoIOperationProperties : OperationProperties
    {
        public ModeEnum Mode { get; set; }

        public enum ModeEnum
        {
            MarkBlobsToRemove,
            MarkAreaToRemove,
            MarkAreaToRemoveOnAllLayers,
            UnmarkArea,
            AutoMarkOuterBlobsToRemove,
            MarkLayerToRemove,
            RemoveMarkedAreasFromRoi
        }
    }
}
