using Core.Image;
using Core.Interfaces.Operation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Operation
{
    public abstract class PythonOperationBase : IOperation
    {
        public abstract string Name { get; }
        protected abstract string Endpoint { get; }

        protected string webServerAdress = "http://127.0.0.1:8000";

        public abstract OperationProperties DefaultOperationProperties { get; }

        static HttpClient client = new HttpClient();
        public PythonOperationBase()
        {
            client.BaseAddress = new Uri(webServerAdress);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token)
        {
            ContextJson contextJson = new ContextJson();
            contextJson.AddAllLayers(context);

            try
            {
                await SendPost(contextJson.ContextJsonData);
            }
            catch (Exception)
            {
                Debug.WriteLine("Error in calling Python operation: " + Endpoint);
            }
            return context;
        }

        async Task SendPost(ContextJsonData contextJsonData)
        {
            HttpResponseMessage response = await PostJsonAsync(client, Endpoint, contextJsonData);
            response.EnsureSuccessStatusCode();
        }
        public async Task<HttpResponseMessage> PostJsonAsync<T>(HttpClient client, string requestUri, T value)
        {
            var data = JsonConvert.SerializeObject(value);
            var content = new StringContent(data, Encoding.UTF8, "application/json");
            Debug.WriteLine(client.BaseAddress + requestUri);
            return await client.PostAsync(requestUri, content).ConfigureAwait(false);
        }
    }

    public class ContextJsonData
    {
        public List<int[,]> BlobImages = new List<int[,]>();
        public List<int[,]> RawImages = new List<int[,]>();
        public Dictionary<int, List<Tag>> Tags = new Dictionary<int, List<Tag>>();
    }

    public class ContextJson
    {
        public ContextJsonData ContextJsonData = new ContextJsonData();

        public void AddActiveLayer(OperationContext context)
        {
            AddLayer(context, context.ActiveLayer);
        }

        public void AddAllLayers(OperationContext context)
        {
            for (int layer = 0; layer < context.RawImageMetadata.NumberOfLayers; layer++)
                AddLayer(context, layer);
        }

        public void AddLayer(OperationContext context, int layer)
        {
            var size = context.ActiveBlobImage.Size;
            var blobImage = context.BlobImages[layer];
            var rawImageBitmap = context.RawImages[layer];
            int[,] image = new int[size.Width, size.Width];
            for (int x = 0; x < size.Width; x++)
                for (int y = 0; y < size.Height; y++)
                    image[x, y] = blobImage[x, y];
            ContextJsonData.BlobImages.Add(image);
            ContextJsonData.RawImages.Add(BitmapToArray2D(rawImageBitmap));
            blobImage.Tags.ToList().ForEach(x => ContextJsonData.Tags[x.Key] = x.Value);
        }

        public static int[,] BitmapToArray2D(Bitmap image)
        {
            byte[,] array2D = new byte[image.Width, image.Height];
            int[,] array2Dint = new int[image.Width, image.Height];
            for (int i = 0; i < image.Height; i++)
                for (int j = 0; j < image.Width; j++)
                    array2Dint[i, j] = (int)(image.GetPixel(j, i).GetBrightness()*255);

            return array2Dint;

            using (var stream = new MemoryStream())
            {
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                byte[] array1D = stream.ToArray();
                Buffer.BlockCopy(array1D, 0, array2D, 0, array1D.Length);
                for (int i = 0; i < image.Height; i++)
                    for (int j = 0; j < image.Width; j++)
                        array2Dint[i, j] = array2D[i, j];
                return array2Dint;
            }
        }
    }
}
