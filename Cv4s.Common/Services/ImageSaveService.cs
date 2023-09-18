using Cv4s.Common.Exceptions;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Models;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Imaging;

namespace Cv4s.Common.Services
{
    public class ImageSaveService : IImageSaveService
    {
        public string CreateDirectoryPath(string where, string newDirectoryName)
        {
            string? dpath = Path.GetDirectoryName(where);

            if (dpath == null)
                throw new BusinessException($"Can't find given path directory: {where}");

            dpath = Path.Combine(dpath, newDirectoryName);

            if (!Directory.Exists(dpath))
                Directory.CreateDirectory(dpath);

            return dpath;
        }

        public string[] SaveFilesToDirectory(string directory, ICollection<Bitmap> images)
        {
            Console.WriteLine($"Saving files to directory: {directory}");

            string[] paths = new string[images.Count];

            int index = 1;
            string baseName = Path.Combine(directory, "s_");

            foreach (var image in images)
            {
                string name = baseName + index.ToString("0000") + ".png";
                image.Save(name, ImageFormat.Png);
                paths[index - 1] = name;
                index++;
            }

            Console.WriteLine("Images Saved!");

            return paths;
        }

        public void SaveResolutionsToDirectory(string directory, double xRes, double yRes, double zRes, string fileName)
        {
            JsonResolution resolution = new JsonResolution
            {
                XResolution = xRes,
                YResolution = yRes,
                ZResolution = zRes
            };

            var jsonResolution = JsonConvert.SerializeObject(resolution);

            File.WriteAllText(Path.Combine(directory, fileName), jsonResolution);
        }
    }
}
