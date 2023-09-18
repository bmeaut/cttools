using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto
{
    public class MaterialScanDto
    {
        public int Id { get; set; }

        public ScanFileFormat ScanFileFormat { get; set; }

        public string[] ScanFilePaths { get; set; }
    }
}
