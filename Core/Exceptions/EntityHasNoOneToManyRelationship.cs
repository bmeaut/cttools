using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Exceptions
{
    public class EntityHasNoOneToManyRelationship<TSrc, TDest> : Exception
        where TSrc : class
        where TDest : class
    {
        private readonly int _entityId;

        public override string Message => $"{typeof(TSrc).Name} with id ({_entityId}) has no entities of type {typeof(TDest).Name}!";

        public EntityHasNoOneToManyRelationship(int entityId)
        {
            _entityId = entityId;
        }
    }
}
