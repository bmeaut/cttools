using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Workspaces
{
    // TODO: return type, handle filesystem, onedrive, etc.
    public class ScanFile : BaseEntity
    {
        public int Id { get; set; }

        public string FilePath { get; set; }
    }
}
