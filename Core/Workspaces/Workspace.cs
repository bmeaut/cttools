using System;
using System.Collections.Generic;

namespace Core.Workspaces
{
    public class Workspace : BaseEntity
    {
        public int Id { get; set; }

        // TODO: megrendelo, hatarido, cim ==> name, leiras ==> description, erkezesi datum, ar ===> Customer/Order object
        public string Name { get; set; }

        public string Description { get; set; }

        public string Customer { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? DayOfArrival { get; set; }

        public decimal? Price { get; set; }


        public int? SessionContextId { get; set; }

        public SessionContext SessionContext { get; set; }

        public IEnumerable<MaterialSample> MaterialSamples { get; set; }

        public int? CurrentStatusId { get; set; }

        public Status CurrentStatus { get; set; }

        public ICollection<Status> Statuses { get; set; }

        // TODO: public IEnumerable<IPlugin> Plugins { get; set; }
    }
}
