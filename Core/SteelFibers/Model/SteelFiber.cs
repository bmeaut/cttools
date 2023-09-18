using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Core.Model.SteelFibers
{
    public class SteelFiber
    {
        public int SteelFiberId { get; set; }

        // a blobok listája, key: kép sorszáma, amin van, value: blobId az adott képen
        public Dictionary<int, int> Blobs { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
