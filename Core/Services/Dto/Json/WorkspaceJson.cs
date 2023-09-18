using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto.Json
{
    public class WorkspaceJson
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Customer { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? DayOfArrival { get; set; }

        public decimal? Price { get; set; }


        public IEnumerable<MaterialSampleJson> MaterialSamples { get; set; }

        public StatusJson CurrentStatus { get; set; }

        public ICollection<StatusJson> Statuses { get; set; }
    }
}
