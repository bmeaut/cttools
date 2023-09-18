using System;

namespace Core.Workspaces
{
    public class Status : BaseEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }


        public int? WorkspaceId { get; set; }

        public Workspace Workspace { get; set; }

        public int? MaterialSampleId { get; set; }

        public MaterialSample MaterialSample { get; set; }
    }
}