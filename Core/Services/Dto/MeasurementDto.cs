using Core.Interfaces.Operation;
using Core.Operation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto
{
    public class MeasurementDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public MaterialSampleDto MaterialSample { get; set; }

        public Dictionary<string, InternalOutput> InternalOutputs { get; set; }

    }
}
