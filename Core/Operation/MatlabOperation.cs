using Core.Enums;
using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class MatlabOperation : IOperation
    {
        public string Name { get; set; }

        public OperationType OperationType => OperationType.MATLAB_OPERATION;

        public OperationProperties DefaultOperationProperties { get; set; }

        public string OperationPath { get; set; }

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
#if MATLAB_CONNECTED_VIA_COM    // Define in first row of file and add MatLAB as accessible COM component to the Core project.
            MLApp.MLApp matlab = new MLApp.MLApp();
            matlab.Visible = 0;

            //load matlab script
            matlab.Execute($"cd { OperationPath }");

            //serialize
            using (StreamWriter file = File.CreateText($"{ OperationPath }\\op_context.json"))
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, WrapOperationContext(context));
            }

            //run
            object results = null;
            matlab.Feval("run", 1, out results);
            object[] res = results as object[];

            // deserialize and get results
            var wrappedContext = JsonConvert.DeserializeObject<ContextWrapper>(res[0] as string);
            UnwrapOperationContext(context, wrappedContext);

            return context;
#else
            throw new InvalidOperationException("This version of the application is compiled without MatLAB support.");
#endif
        }

        private class ContextWrapper
        {
            public int ActiveLayer { get; set; }
            public dynamic OperationProperties { get; set; }
            public List<BlobImageWrapper> BlobImages { get; set; }
            public RawImageMetadata RawImageMetadata { get; set; }
        }

        private class BlobImageWrapper
        {
            public int[,] BlobImage { get; set; }

            [JsonProperty(ItemConverterType = typeof(SingleOrArrayConverter<Tag>))]
            public Dictionary<string, List<Tag>> Tags { get; set; } // IEnumerable<Tag>
        }

        class SingleOrArrayConverter<T> : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(List<T>));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JToken token = JToken.Load(reader);
                if (token.Type == JTokenType.Array)
                {
                    return token.ToObject<List<T>>();
                }
                return new List<T> { token.ToObject<T>() };
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }


        private ContextWrapper WrapOperationContext(OperationContext context)
        {
            var size = context.BlobImages[0].Size;
            List<BlobImageWrapper> labelImages = new List<BlobImageWrapper>();
            foreach (var blobImage in context.BlobImages)
            {
                // collect blobids
                HashSet<int> blobIds = new HashSet<int>();

                // create blob image representation
                int[,] blobArray = new int[size.Height, size.Width];
                for (int y = 0; y < size.Height; y++)
                {
                    for (int x = 0; x < size.Width; x++)
                    {
                        var blobId = blobImage.Value[y, x];
                        blobArray[y, x] = blobId;
                        blobIds.Add(blobId);
                    }
                }

                // collect and save tags
                Dictionary<string, List<Tag>> tagsForBlobId = new Dictionary<string, List<Tag>>(); // Doesn't support generic ITag interface at the moment. Task for the future. :D
                foreach (var blobId in blobIds)
                {
                    tagsForBlobId.Add($"x{blobId}", blobImage.Value.GetTagsForBlob(blobId).Select(bb => new Tag(bb.Name, bb.Value)).ToList());
                }

                labelImages.Add(
                    new BlobImageWrapper
                    {
                        BlobImage = blobArray,
                        Tags = tagsForBlobId
                    }
                );
            }

            return new ContextWrapper
            {
                ActiveLayer = context.ActiveLayer + 1, // Matlab uses 1-based indexing
                OperationProperties = context.OperationProperties,
                BlobImages = labelImages,
                RawImageMetadata = context.RawImageMetadata
            };
        }

        private void UnwrapOperationContext(OperationContext context, ContextWrapper wrappedContext)
        {
            // replay changes on proxy
            for (int idx = 0; idx < wrappedContext.BlobImages.Count; idx++)
            {
                // update blobimage
                for (int y = 0; y < context.BlobImages[0].Size.Height; y++)
                {
                    for (int x = 0; x < context.BlobImages[0].Size.Width; x++)
                    {
                        context.BlobImages[idx][y, x] = wrappedContext.BlobImages[idx].BlobImage[y, x];
                    }
                }

                // update tags
                foreach (var blobIdTag in wrappedContext.BlobImages[idx].Tags)
                {
                    if (blobIdTag.Value.Count == 0) continue;

                    var blobId = int.Parse(blobIdTag.Key.Remove(0, 1));
                    List<Tag> tags = blobIdTag.Value;

                    foreach (var tag in tags)
                    {
                        context.BlobImages[idx].SetTag(blobId, tag.Name, tag.Value);
                    }

                }
            }
        }
    }
}
