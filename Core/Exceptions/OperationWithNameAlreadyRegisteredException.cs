using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Exceptions
{
    public class OperationWithNameAlreadyRegisteredException : Exception
    {
        private readonly string _name;

        public override string Message => $"Operation with name ({_name}) already registered.";

        public OperationWithNameAlreadyRegisteredException(string name)
        {
            _name = name;
        }
    }
}
