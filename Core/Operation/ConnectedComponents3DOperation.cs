using Core.Enums;
using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class ConnectedComponents3DOperation : IOperation
    {
        public string Name => "ConnectedComponents3D";

        public OperationProperties DefaultOperationProperties => new ConnectedComponents3DOperationProperties();

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            var blobImages = context.BlobImages;
            var properties = context.OperationProperties as ConnectedComponents3DOperationProperties;
            var material = properties.Material.ToString();
            var neighbourCollectingProgress = new Progress<double>();
            neighbourCollectingProgress.ProgressChanged += (_, p) => progress.Report(p * 0.9);
            var neighboursGraph = CollectNeighboursGraph(blobImages, material, neighbourCollectingProgress);

            int materialId = 1;
            while (neighboursGraph.Count > 0)
            {
                var materialBlobs = new Queue<LayerBlob>();
                materialBlobs.Enqueue(neighboursGraph.Keys.First());
                while (materialBlobs.Count > 0)
                {
                    var layerBlob = materialBlobs.Dequeue();
                    if (!neighboursGraph.ContainsKey(layerBlob))
                    {
                        continue;
                    }
                    blobImages[layerBlob.Layer].SetTag(layerBlob.Blob, material, materialId);
                    var neighbours = neighboursGraph[layerBlob];
                    neighboursGraph.Remove(layerBlob);
                    foreach (var neighbour in neighbours)
                    {
                        if (neighboursGraph.ContainsKey(neighbour))
                        {
                            materialBlobs.Enqueue(neighbour);
                        }
                    }
                }
                materialId++;
            }

            return context;
        }

        private Dictionary<LayerBlob, List<LayerBlob>> CollectNeighboursGraph(
            IDictionary<int, IBlobImage> blobImages, string material, IProgress<double> progress)
        {
            var neighboursGraph = new Dictionary<LayerBlob, List<LayerBlob>>();

            int done = 0;
            for (int z = 0; z < blobImages.Count - 1; z++)
            {
                var blobImage = blobImages[z];
                for (int x = 0; x < blobImage.Size.Width; x++)
                {
                    for (int y = 0; y < blobImage.Size.Height; y++)
                    {
                        var blobId = blobImage[y, x];
                        if (HasTag(blobImage, blobId, material))
                        {
                            var layerBlob = new LayerBlob(z, blobId);

                            AddToNeighbours(neighboursGraph, layerBlob, layerBlob);

                            var neighbourBlobImage = blobImages[z + 1];
                            var neighbourBlobId = neighbourBlobImage[y, x];
                            if (HasTag(neighbourBlobImage, neighbourBlobId, material))
                            {
                                var neighbourLayerBlob = new LayerBlob(z + 1, neighbourBlobId);
                                AddToNeighbours(neighboursGraph, layerBlob, neighbourLayerBlob);
                                AddToNeighbours(neighboursGraph, neighbourLayerBlob, layerBlob);
                            }
                        }
                    }
                }
                done++;
                progress.Report(done / (double)blobImages.Count);

            }
            var lastLayer = blobImages.Count - 1;
            var lastBlobImage = blobImages[lastLayer];
            var lastLayerMaterialBlobs = lastBlobImage.CollectAllRealBlobIds().Where(b => HasTag(lastBlobImage, b, material));
            foreach (var blob in lastLayerMaterialBlobs)
            {
                var layerBlob = new LayerBlob(lastLayer, blob);
                AddToNeighbours(neighboursGraph, layerBlob, layerBlob);
            }
            progress.Report(1);

            return neighboursGraph;
        }

        private void AddToNeighbours(Dictionary<LayerBlob, List<LayerBlob>> neighbours, LayerBlob key, LayerBlob value)
        {
            if (!neighbours.ContainsKey(key))
            {
                neighbours.Add(key, new List<LayerBlob>());
            }
            if (!neighbours[key].Contains(value))
            {
                neighbours[key].Add(value);
            }
        }

        private bool HasTag(IBlobImage image, int blob, string tagname)
        {
            if (blob == 0)
            {
                return false;
            }
            else
            {
                return image.GetTagsForBlob(blob).Any(t => t.Name == tagname);
            }
        }

        private struct LayerBlob
        {
            public int Layer;
            public int Blob;

            public LayerBlob(int layer, int blob) => (Layer, Blob) = (layer, blob);
        }
    }

    public class ConnectedComponents3DOperationProperties : OperationProperties
    {
        public MaterialTags Material { get; set; }
    }
}
