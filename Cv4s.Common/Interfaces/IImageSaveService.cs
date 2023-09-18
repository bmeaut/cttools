using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cv4s.Common.Interfaces
{
    public interface IImageSaveService
    {
        /// <summary>
        /// Saves the given <paramref name="images"/> to the <paramref name="directory"/>
        /// </summary>
        /// <param name="directory">the directory path</param>
        /// <param name="images">saveable images</param>
        /// <returns> the path to the saved files</returns>
        public string[] SaveFilesToDirectory(string directory, ICollection<System.Drawing.Bitmap> images);

        /// <summary>
        /// Creates the <paramref name="newDirectoryName"/> directory in the given  <paramref name="where"/> path 
        /// </summary>
        /// <param name="where"></param>
        /// <param name="newDirectoryName"></param>
        /// <returns> returns the new path to the folder</returns>
        public string CreateDirectoryPath(string where, string newDirectoryName);


        public void SaveResolutionsToDirectory(string directory, double xRes, double yRes, double zRes, string fileName);
    }
}
