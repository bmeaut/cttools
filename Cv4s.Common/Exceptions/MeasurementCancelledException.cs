using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cv4s.Common.Exceptions
{
    public class MeasurementCancelledException : Exception
    {
        public MeasurementCancelledException() : base("Measurement is cancelled during process") { }
    }
}
