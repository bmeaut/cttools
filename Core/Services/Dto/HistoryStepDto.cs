using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto
{
    public class HistoryStepDto
    {
        public string DisplayName { get; set; }
        public bool IsActive { get; set; }
        public bool IsCurrent { get; set; }
    }
}
