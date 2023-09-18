using Core.Enums;
using Core.Interfaces.Operation;
using Core.Operation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto
{
    public class OperationDto
    {
        public string Name { get; set; }

        public OperationType OperationType { get; set; }

        public OperationProperties DefaultOperationProperties { get; set; }
    }
}
