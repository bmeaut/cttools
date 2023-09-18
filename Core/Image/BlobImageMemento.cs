using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Image
{
    /// <summary>
    /// Meant to be stored as undo information.
    /// If undo needs to be applied, it can create a Proxy instance to do that.
    /// </summary>
    public class BlobImageMemento
    {
        // The changeset which needs to be applied to undo the previous operation.
        // (This changeset is already the inverse of the former operation.)
        private readonly BlobImageChangeSet inverseChangeSet;

        public BlobImageMemento(BlobImage original)
        {
            this.inverseChangeSet = new BlobImageChangeSet(original);
        }

        internal BlobImageMemento(BlobImageChangeSet inverseChangeSet)
        {
            this.inverseChangeSet = inverseChangeSet;
        }

        /// <summary>
        /// Returns a proxy which can apply the undo changes stored in this memento.
        /// </summary>
        /// <returns></returns>
        public BlobImageProxy CreateUndoProxy()
        {
            return new BlobImageProxy(inverseChangeSet);
        }

        public bool IsIdentity => inverseChangeSet.IsIdentity;
    }
}
