using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cv4s.Operations.PoreSizeStatOperation.Models
{
    public class LayerPore
    {
        public int layerId { get; set; }
        public int blobId { get; set; }
        public bool processed { get; set; } = false;
    }
}
