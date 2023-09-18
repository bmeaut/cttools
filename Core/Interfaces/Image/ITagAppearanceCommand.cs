using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.Image
{
    public interface ITagAppearanceCommand
    {
        public int Priority { get; set; }
        public string TagName { get; }
        public IColorProviderForTag ColorProviderForTag { get; }
    }
}
