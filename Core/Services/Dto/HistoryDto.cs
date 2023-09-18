using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto
{
    public class HistoryDto
    {
        public int CurrentStep { get; set; }
        public IEnumerable<HistoryStepDto> History { get; set; }
    }
}
