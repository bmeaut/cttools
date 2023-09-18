using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto.Json
{
    public class MaterialSampleJson
    {
        public int Id { get; set; }

        public string Label { get; set; }


        public MaterialScanJson MaterialScan { get; set; }

        public IEnumerable<UserGeneratedFileJson> UserGeneratedFiles { get; set; }

        public IEnumerable<MeasurementJson> Measurements { get; set; }

        public StatusJson CurrentStatus { get; set; }

        public ICollection<StatusJson> Statuses { get; set; }
    }
}
