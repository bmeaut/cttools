using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces.Operation
{
    public interface IPlugin
    {
        public IOperation Operation { get; set; }
    }
}
