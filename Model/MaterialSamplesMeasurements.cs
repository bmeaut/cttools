using Core.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class MaterialSamplesMeasurements
    {
        public MaterialSampleDto MaterialSample { get; set; }

        public List<MeasurementDto> Measurements { get; set; }

        // TODO Bind this to TreeViewItem IsExpanded property
        public bool IsExpanded { get; set; }
    }
}
