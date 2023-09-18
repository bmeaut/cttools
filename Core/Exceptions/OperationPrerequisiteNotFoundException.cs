using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Exceptions
{
    class OperationPrerequisiteNotFoundException : Exception
    {
        private readonly string _name;
        private readonly string _prequisite;


        public override string Message => $"Operation prequisite for operation ({_name}) not found. Prequisite is: {_prequisite}";

        public OperationPrerequisiteNotFoundException(string operationName, string prequisite)
        {
            _name = operationName;
            _prequisite = prequisite;
        }
    }
}
