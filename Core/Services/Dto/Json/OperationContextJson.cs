using Core.Interfaces.Operation;
using Core.Operation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto.Json
{
    public class OperationContextJson
    {
        public int Id { get; set; }

        public string OperationName { get; set; }

        public int ActiveLayer { get; set; }

        public int MeasurementId { get; set; }

        public OperationProperties OperationProperties { get; set; }

        public Dictionary<string, InternalOutput> InternalOutputs { get; set; }
    }
}
