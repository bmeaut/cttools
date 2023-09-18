using Cv4s.Common.Extensions;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;

namespace Cv4s.Operations.ConnectedComponents3DOperation
{
    public class ConnectedComponents3DOperation : OperationBase
    {
        private readonly IUIHandlerService _uIHandlerService;

        public ConnectedComponents3DOperation(IUIHandlerService uIHandlerService)
        {
            _uIHandlerService = uIHandlerService;

            Properties = new ConnectedComponents3DOperationProperties();

            RunEventArgs.IsCallableFromButton = true;
            RunEventArgs.IsCallableFromCanvas = false;
        }


        public IBlobImageSource ConnectComponents(IRawImageSource rawImages, IBlobImageSource blobImages)
        {
            _uIHandlerService.ShowMeasurementEditor(this, rawImages, blobImages);

            var properties = Properties as ConnectedComponents3DOperationProperties;
            
            var material = properties.Material.ToString();

            var neighboursGraph = CollectNeighboursGraph(blobImages, material);

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

            return blobImages;
        }

        private Dictionary<LayerBlob, List<LayerBlob>> CollectNeighboursGraph(
                           IBlobImageSource blobImages, string material)
        {
            var neighboursGraph = new Dictionary<LayerBlob, List<LayerBlob>>();

            for (int z = 0; z < blobImages.Keys.Count - 1; z++)
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
            }
            var lastLayer = blobImages.Keys.Count - 1;
            var lastBlobImage = blobImages[lastLayer];
            var lastLayerMaterialBlobs = lastBlobImage.CollectAllRealBlobIds().Where(b => HasTag(lastBlobImage, b, material));
            foreach (var blob in lastLayerMaterialBlobs)
            {
                var layerBlob = new LayerBlob(lastLayer, blob);
                AddToNeighbours(neighboursGraph, layerBlob, layerBlob);
            }

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

}
