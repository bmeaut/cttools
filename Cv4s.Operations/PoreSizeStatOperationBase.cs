using Cv4s.Common.Enums;
using Cv4s.Common.Exceptions;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Common.Models;
using Cv4s.Operations.PoreSizeStatOperation.Models;

namespace Cv4s.Operations
{
    public delegate void SievesValueAdder(double key, SizeVolume pore);

    public abstract class PoreSizeStatOperationBase : OperationBase
    {
        //protected JsonResolution Resolutions;
        protected Dictionary<int, List<LayerPore>> allBlobIds = new Dictionary<int, List<LayerPore>>();
        protected Dictionary<double, double> Sieves = new Dictionary<double, double>(); //szitameretek es a tomegszazalekarany
        protected List<double> Percentages = new List<double>();
        protected Dictionary<int, SizeVolume> GrainSizesAndVolumes = new Dictionary<int, SizeVolume>();

        protected string materialType = MaterialTag.PORE.ToString();

        protected Dictionary<int, SizeVolume> CalculateDistribution(IRawImageSource rawImages, IBlobImageSource blobImages, SievesValueAdder valueAdder, IPoreSizeAlgorithm algorithm)
        {

            //Resolutions = CollectResolutions(context);

            //Init sieves
            for (int i = 0; i < 12; i++)
            {
                var key = Math.Pow(2, i - 4);
                Sieves.Add(key, 0);
            }


            for (int i = 0; i < blobImages.Keys.Count; i++)
            {                                                                     //list<Tag>
                List<int> poreBlobIds = blobImages[i].GetBlobsByTagValue(materialType, null).ToList();
                poreBlobIds.Remove(0); //Removing not RealBlobIds

                foreach (int blobId in poreBlobIds)
                {
                    var poreIds = blobImages[i].GetTagsForBlob(blobId).Where(t => t.Name.Equals(materialType)).Select(t => t.Value).ToList();
                    if (poreIds.Count > 1)
                        throw new Exception("Different tags for the same blob");

                    if (poreIds.Count != 0 && poreIds[0] >= 1)
                    {
                        LayerPore pore = new LayerPore { layerId = i, blobId = blobId };
                        if (!allBlobIds.ContainsKey(poreIds[0]))
                            allBlobIds.Add(poreIds[0], new List<LayerPore>());
                        allBlobIds[poreIds[0]].Add(pore);
                    }
                }
            }

            GrainSizesAndVolumes = algorithm.Calculate(allBlobIds, blobImages,
                                            rawImages.XResolution,
                                            rawImages.YResolution,
                                            rawImages.ZResolution);

            if (GrainSizesAndVolumes.Count == 0)
                throw new BusinessException("Cant find any size volume pair!!");

            foreach (var pore in GrainSizesAndVolumes.Select(s => s.Value).ToList())
            {
                for (int i = 0; i < Sieves.Count - 1; i++)
                {
                    var prev = Sieves.ElementAt(i);
                    var next = Sieves.ElementAt(i + 1);
                    if (pore.Size >= prev.Key && pore.Size <= next.Key)
                    {
                        valueAdder?.Invoke(prev.Key, pore);
                        break;
                    }

                    if (i == (Sieves.Count - 2))//we are at the end of the dictionary and there is no place for our pore/greain
                        break;
                }
            }

            return GrainSizesAndVolumes;
        }

    }
}
