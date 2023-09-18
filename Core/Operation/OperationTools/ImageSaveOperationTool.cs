using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Operation.OperationTools
{
    /// <summary>
    /// A class which helps with saving files and resolutions to the specified folder
    /// </summary>
    public static class ImageSaveOperationTool
    {

        public static void SaveResolutionsToDirectory(string directory, double xRes, double yRes, double zRes, string fileName)
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

        /// <summary>
        /// Creates the <paramref name="newDirectoryName"/> directory in the given  <paramref name="where"/> path 
        /// </summary>
        /// <param name="where"></param>
        /// <param name="newDirectoryName"></param>
        /// <returns> returns the new path to the folder</returns>
        public static string CreateDirectoryPath(string where, string newDirectoryName)
        {
            string dpath = Path.GetDirectoryName(where);
            dpath = Path.Combine(dpath, newDirectoryName);

            if (!Directory.Exists(dpath))
                Directory.CreateDirectory(dpath);

            return dpath;
        }

        /// <summary>
        /// Saves the given <paramref name="images"/> to the <paramref name="directory"/>
        /// </summary>
        /// <param name="directory">the directory path</param>
        /// <param name="images">saveable images</param>
        /// <returns> the path to the saved files</returns>
        public static string[] SaveFilesToDirectory(string directory, ICollection<System.Drawing.Bitmap> images)
        {
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

            return paths;
        }

    }
}
