using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public interface ICommunicationMediator
    {
        public int LastCreatedMaterialSampleId {get; set;}
        public int CloningMeasurementId { get; set; }
        public Action PlotCountChanged { get; set; }
    }
}
