using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Exceptions
{
    public class LayerIndexOutOfBoundsException : Exception
    {
        private readonly int _layerIndex;

        public override string Message => $"Layer index {_layerIndex} is out of bounds!";

        public LayerIndexOutOfBoundsException(int layerIndex)
        {
            _layerIndex = layerIndex;
        }
    }
}
