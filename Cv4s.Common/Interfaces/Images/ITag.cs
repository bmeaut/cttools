using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cv4s.Common.Interfaces.Images
{
    public interface ITag<T>
    {
        public string Name { get; set; }

        public T Value { get; set; }
    }
}
