using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Exceptions
{
    public class NoMeasurementOpenedException : Exception
    {
        public override string Message => "There is no currently opened measurement!";
    }
}
