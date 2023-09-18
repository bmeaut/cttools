using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.SteelFibers.Algorithm
{
    public interface IAlgorithm
    {
        public Point[] Solve(double[,] originalWeights);
    }
}
