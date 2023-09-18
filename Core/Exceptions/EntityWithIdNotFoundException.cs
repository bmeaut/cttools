using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Exceptions
{
    public class EntityWithIdNotFoundException<T> : Exception where T : class
    {
        private readonly int _missingId;

        public override string Message => $"{typeof(T)} Entity with ID {_missingId} not found!";

        public EntityWithIdNotFoundException(int missingId)
        {
            _missingId = missingId;
        }
    }
}
