using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    /// <summary>
    /// Mediator class based on mediator pattern for messaging between VM-s.
    /// Consider using more advanced Messaging service in the future.
    /// </summary>
    public class SimpleCommunicationMediator : ICommunicationMediator
    {
        public int LastCreatedMaterialSampleId { get; set; }
        public int CloningMeasurementId { get; set; }
        public Action PlotCountChanged { get; set; } = delegate () { Debug.WriteLine("PlotCountChanged"); };
    }
}
