using System.Collections.Generic;
using Core.Enums;
using Core.Interfaces.Image;
using Core.Workspaces;

namespace Core.Services.Dto
{
    public class MaterialSampleDto
    {
        public int Id { get; set; }

        public string Label { get; set; }

        public StatusDto CurrentStatus { get; set; }

        public int WorkspaceId { get; set; }

        public MaterialScanDto MaterialScan { get; set; }

        public IRawImageSource RawImages { get; set; }
    }
}