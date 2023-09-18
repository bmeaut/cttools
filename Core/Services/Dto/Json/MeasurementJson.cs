using Core.Image;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services.Dto.Json
{
    public class MeasurementJson
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }


        public IEnumerable<OperationContextJson> OperationContexts { get; set; }

        public IEnumerable<BlobImageEntity> BlobImageEntities { get; set; }
    }
}
