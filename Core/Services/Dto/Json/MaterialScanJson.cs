using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto.Json
{
    public class MaterialScanJson
    {
        public int Id { get; set; }

        public ScanFileFormat ScanFileFormat { get; set; }

        public ICollection<ScanFileJson> ScanFiles { get; set; }
    }
}
