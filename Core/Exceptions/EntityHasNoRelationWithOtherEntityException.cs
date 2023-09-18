using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Exceptions
{
    public class EntityHasNoRelationWithOtherEntityException<TSrc, TDst> : Exception
        where TSrc : class
        where TDst : class
    {
        private int _srcEntityId;
        private int _dstEntityId;

        public override string Message => $"{typeof(TSrc).Name} with id {_srcEntityId} has no relationship with {typeof(TDst).Name} with id {_dstEntityId}!";

        public EntityHasNoRelationWithOtherEntityException(int srcEntityId, int dstEntityId)
        {
            _srcEntityId = srcEntityId;
            _dstEntityId = dstEntityId;
        }
    }
}
