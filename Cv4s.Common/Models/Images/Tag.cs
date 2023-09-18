using Cv4s.Common.Interfaces.Images;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cv4s.Common.Models.Images
{
    /// <summary>
    /// Represents a key-value pair which can be put on blobs.
    /// </summary>
    public class Tag : ITag<int>
    {
        public string Name { get; set; }
        public int Value { get; set; }

        public Tag(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }
}
