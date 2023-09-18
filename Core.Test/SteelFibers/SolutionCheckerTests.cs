using Core.Model.SteelFibers;
using Core.SteelFibers.SolutionChecker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Core.Test.SteelFibers
{
    public class SolutionCheckerTests
    {
        [Fact]
        public void SolutionCorrectTest()
        {
            Dictionary<int, int> blobs0 = new Dictionary<int, int>
            {
                { 1, 2 },
                { 2, 2 },
                { 3, 2 }
            };
            Dictionary<int, int> blobs1 = new Dictionary<int, int>
            {
                { 4, 3 },
                { 5, 3 },
                { 6, 4 }
            };
            SolutionChecker checker = new();
            checker.expectedSteelFibers = new List<SteelFiber>
            {
                new SteelFiber() { SteelFiberId = 0, Blobs = blobs0 },
                new SteelFiber() { SteelFiberId = 1, Blobs = blobs1 }
            };
            checker.actualSteelFibers = new List<SteelFiber>
            {
                new SteelFiber() { SteelFiberId = 0, Blobs = blobs0 },
                new SteelFiber() { SteelFiberId = 1, Blobs = blobs1 }
            };

            var result = checker.CheckSoluitons();
            var expected = "Hibák száma: 0\nPontosság: 100%\n";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TooManySteelFibersTest()
        {
            Dictionary<int, int> blobs0 = new Dictionary<int, int>
            {
                { 1, 2 },
                { 2, 2 },
                { 3, 2 }
            };
            Dictionary<int, int> blobs1 = new Dictionary<int, int>
            {
                { 4, 3 },
                { 5, 3 },
                { 6, 4 }
            };
            Dictionary<int, int> blobs2 = new Dictionary<int, int>
            {
                { 7, 3 },
                { 8, 3 },
                { 9, 2 }
            };
            SolutionChecker checker = new();
            checker.expectedSteelFibers = new List<SteelFiber>
            {
                new SteelFiber() { SteelFiberId = 0, Blobs = blobs0 },
                new SteelFiber() { SteelFiberId = 1, Blobs = blobs1 }
            };
            checker.actualSteelFibers = new List<SteelFiber>
            {
                new SteelFiber() { SteelFiberId = 0, Blobs = blobs0 },
                new SteelFiber() { SteelFiberId = 1, Blobs = blobs1 },
                new SteelFiber() { SteelFiberId = 2, Blobs = blobs2 }
            };

            var result = checker.CheckSoluitons();
            var expected = $"Acélszálak várt száma: {checker.expectedSteelFibers.Count}\n" +
                $"Acélszálak valós száma: {checker.actualSteelFibers.Count}\n" + 
                $"Hibás acélszál: {checker.actualSteelFibers.Last().ToString()}\n" + 
                "Hibák száma: 1\nPontosság: 66,67%\n";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IncorrectSteelFiberIdTest()
        {
            Dictionary<int, int> blobs0 = new Dictionary<int, int>
            {
                { 1, 2 },
                { 2, 2 },
                { 3, 2 }
            };
            Dictionary<int, int> blobs1 = new Dictionary<int, int>
            {
                { 4, 3 },
                { 5, 3 },
                { 6, 4 }
            };
            SolutionChecker checker = new();
            checker.expectedSteelFibers = new List<SteelFiber>
            {
                new SteelFiber() { SteelFiberId = 0, Blobs = blobs0 },
                new SteelFiber() { SteelFiberId = 1, Blobs = blobs1 }
            };
            checker.actualSteelFibers = new List<SteelFiber>
            {
                new SteelFiber() { SteelFiberId = 2, Blobs = blobs0 },
                new SteelFiber() { SteelFiberId = 1, Blobs = blobs1 }
            };

            var result = checker.CheckSoluitons();
            var expected = $"Hibás acélszál: {checker.actualSteelFibers.First().ToString()}\n" + "Hibák száma: 1\nPontosság: 50%\n";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IncorrectBlobsTest()
        {
            Dictionary<int, int> blobs0 = new Dictionary<int, int>
            {
                { 1, 2 },
                { 2, 2 },
                { 3, 2 }
            };
            Dictionary<int, int> blobs1 = new Dictionary<int, int>
            {
                { 4, 3 },
                { 5, 3 },
                { 6, 4 }
            };
            Dictionary<int, int> blobs1Wrong = new Dictionary<int, int>
            {
                { 4, 3 },
                { 5, 3 },
                { 6, 1 }
            };
            SolutionChecker checker = new();
            checker.expectedSteelFibers = new List<SteelFiber>
            {
                new SteelFiber() { SteelFiberId = 0, Blobs = blobs0 },
                new SteelFiber() { SteelFiberId = 1, Blobs = blobs1 }
            };
            checker.actualSteelFibers = new List<SteelFiber>
            {
                new SteelFiber() { SteelFiberId = 0, Blobs = blobs0 },
                new SteelFiber() { SteelFiberId = 1, Blobs = blobs1Wrong }
            };

            var result = checker.CheckSoluitons();
            var expected = $"Hibás acélszál: {checker.actualSteelFibers[1].ToString()}\n" + "Hibák száma: 1\nPontosság: 50%\n";
            Assert.Equal(expected, result);
        }
    }
}
