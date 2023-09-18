using Core.Enums;
using System.Collections.Generic;

namespace Core.Workspaces
{
    public class MaterialScan : BaseEntity
    {
        public int Id { get; set; }

        public ScanFileFormat ScanFileFormat { get; set; }


        public ICollection<ScanFile> ScanFiles { get; set; }

        public int MaterialSampleId { get; set; }

        public MaterialSample MaterialSample { get; set; }
    }
}