using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto
{
    public class SessionDto
    {
        public WorkspaceDto CurrentWorkspace { get; set; }

        public MeasurementDto CurrentMeasurement { get; set; }

        public int CurrentLayer { get; set; }
    }
}
