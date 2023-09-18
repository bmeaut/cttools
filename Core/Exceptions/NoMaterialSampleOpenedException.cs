using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Exceptions
{
    public class NoMaterialSampleOpenedException : Exception
    {
        public override string Message => "There is no currently opened material sample!";
    }
}
