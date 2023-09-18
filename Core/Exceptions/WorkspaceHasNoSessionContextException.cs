using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Exceptions
{
    public class WorkspaceHasNoSessionContextException : Exception
    {
        private readonly int _workspaceId;

        public override string Message => $"Workspace with id {_workspaceId} has no saved session context.";

        public WorkspaceHasNoSessionContextException(int workspaceId)
        {
            _workspaceId = workspaceId;
        }
    }
}
