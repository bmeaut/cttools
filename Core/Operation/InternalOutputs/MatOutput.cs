using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Drawing.Imaging;
using Newtonsoft.Json.Linq;
using OpenCvSharp.Extensions;

namespace Core.Operation.InternalOutputs
{
    /// <summary>
    /// MatOutput is a Wrapper for Mat objects that is Serializable. Mats are stored in the Values property.
    /// Index values should be in a range 0-1. These values will be scaled to 0-255 when creating bitmap in GetBitmap() method.
    /// 
    /// Serialization is working as it serializes primitives and builds up from them again in getter method.
    /// This is mandatory because neither Mat or Bitmap objects can be serialized easily.
    /// If converters for these classes will exist in the future, this wrapping can be simplified 
    /// </summary>
    public class MatOutput : InternalOutput
    {
        private Mat[] _values;

        // These are serializable and Distancevalues can be instantiated from these
        public List<float[,]> _indexerValues;
        public int XSize;
        public int YSize;
        public int ZSize;

        [JsonIgnore]
        public Mat[] Values
        {
            get
            {
                if (_values == null)
                {
                    if (ZSize > 0 && _indexerValues != null)
                    {
                        _values = new Mat[ZSize];
                        for (int z = 0; z < ZSize; z++)
                        {
                            Mat m = new Mat(YSize, XSize, MatType.CV_32F, _indexerValues[z]);
                            _values[z] = m;
                        }
                    }
                }
                return _values;
            }
            set
            {
                if (value.Length > 0)
                {
                    var mat = value[0];
                    ZSize = value.Length;
                    XSize = mat.Width;
                    YSize = mat.Height;
                    _indexerValues = new List<float[,]>();
                    for (int z = 0; z < value.Length; z++)
                    {
                        Mat m = value[z];
                        var indexer = m.GetGenericIndexer<float>();
                        var array = new float[YSize, XSize];
                        for (int x = 0; x < m.Width; x++)
                            for (int y = 0; y < m.Height; y++)
                            {
                                var test = indexer[y, x];
                                array[y, x] = test;
                            }
                        _indexerValues.Add(array);
                    }
                }

            }
        }

        // Converts inner structure to CV_8U and produces Bitmap from it.
        public Bitmap GetBitmap(int layer)
        {
            Mat m = Values[layer];
            Mat m1 = new Mat();
            Cv2.Normalize(m, m1, 0, 255, NormTypes.MinMax);

            Mat m2 = new Mat(XSize, YSize, MatType.CV_8UC1);
            m1.ConvertTo(m2, MatType.CV_8UC1);
            return m2.ToBitmap();
        }
    }

}
