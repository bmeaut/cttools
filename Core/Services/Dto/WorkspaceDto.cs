using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto
{
    public class WorkspaceDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Customer { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? DayOfArrival { get; set; }

        public decimal? Price { get; set; }

        public StatusDto CurrentStatus { get; set; }

        public bool HasSavedSession { get; set; }
    }
}
