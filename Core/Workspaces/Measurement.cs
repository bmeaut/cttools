using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Operation;
using System.Collections.Generic;

namespace Core.Workspaces
{
    public class Measurement : BaseEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }


        public int MaterialSampleId { get; set; }

        public MaterialSample MaterialSample { get; set; }

        public IEnumerable<OperationContext> OperationContexts { get; set; }

        public IEnumerable<BlobImageEntity> BlobImageEntities
        {
            get
            {
                return BlobImages.GetBlobImageEntities();
            }

            set
            {
                BlobImages = new BlobImageSource(value);
            }
        }


        public IBlobImageSource BlobImages { get; set; }

        public Dictionary<string, InternalOutput> InternalOutputs
        {
            get
            {
                if (OperationContexts == null)
                    return null;

                var dict = new Dictionary<string, InternalOutput>();
                foreach (var context in OperationContexts)
                {
                    if (context.InternalOutputs == null) return dict;
                    foreach (var key in context.InternalOutputs.Keys)
                        dict.TryAdd(key, context.InternalOutputs[key]);
                }
                return dict;
            }
        }
    }
}