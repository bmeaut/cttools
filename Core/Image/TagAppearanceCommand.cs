using Core.Interfaces.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Image
{
    public class TagAppearanceCommand : ITagAppearanceCommand
    {
        public TagAppearanceCommand(string tagName, IColorProviderForTag colorProviderForTag, int priority)
        {
            Priority = priority;
            TagName = tagName;
            ColorProviderForTag = colorProviderForTag;
        }

        public int Priority { get; set; }
        public string TagName { get; }
        public IColorProviderForTag ColorProviderForTag { get; }
    }
}
