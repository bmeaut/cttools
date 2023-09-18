using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Exceptions
{
    public class WorkspaceImportFileExistsException : Exception
    {
        private readonly string _filePath;

        public override string Message => $"Workspace cannot be imported because cannot extract files to {_filePath} .";

        public WorkspaceImportFileExistsException(string filePath)
        {
            _filePath = filePath;
        }
    }
}
