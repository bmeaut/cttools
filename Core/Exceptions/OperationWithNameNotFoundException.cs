using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Exceptions
{
    public class OperationWithNameNotFoundException : Exception
    {
        private readonly string _name;

        public override string Message => $"Operation with name ({_name}) not found.";

        public OperationWithNameNotFoundException(string name)
        {
            _name = name;
        }
    }
}
