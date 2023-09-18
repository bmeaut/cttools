using Core.Model.SteelFibers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Core.SteelFibers.SolutionChecker
{
    public class SolutionChecker
    {
        public List<SteelFiber> expectedSteelFibers = new();
        public List<SteelFiber> actualSteelFibers = new();

        public SolutionChecker() { }

        public SolutionChecker(string expectedSolutionFile, string actualSolutionFile)
        {
            if (string.IsNullOrEmpty(expectedSolutionFile))
            {
                throw new ArgumentException($"'{nameof(expectedSolutionFile)}' cannot be null or empty", nameof(expectedSolutionFile));
            }

            if (string.IsNullOrEmpty(actualSolutionFile))
            {
                throw new ArgumentException($"'{nameof(actualSolutionFile)}' cannot be null or empty", nameof(actualSolutionFile));
            }

            string jsonString = File.ReadAllText(expectedSolutionFile);
            expectedSteelFibers = JsonSerializer.Deserialize<List<SteelFiber>>(jsonString);
            
            jsonString = File.ReadAllText(actualSolutionFile);
            actualSteelFibers = JsonSerializer.Deserialize<List<SteelFiber>>(jsonString);
        }

        public SolutionChecker(List<SteelFiber> expectedSteelFibers, List<SteelFiber> actualSteelFibers)
        {
            this.expectedSteelFibers = expectedSteelFibers;
            this.actualSteelFibers = actualSteelFibers;
        }

        public string CheckSoluitons()
        {
            string results = String.Empty;
            int errorCounter = 0;
            if (expectedSteelFibers == actualSteelFibers)
            {
                results = "Hibák száma: 0\nPontosság: 100%\n";
            }
            else if (expectedSteelFibers.Count == actualSteelFibers.Count)
            {
                for (int i = 0; i < actualSteelFibers.Count; i++)
                {
                    var temp1 = BlobsAreCorrect(expectedSteelFibers, actualSteelFibers[i]);
                    if (!temp1)
                    {
                        results += $"Hibás acélszál: {actualSteelFibers[i].ToString()}\n";
                        errorCounter++;
                    }
                }
                results += $"Hibák száma: {errorCounter}\n";
                var percentage = (double)(expectedSteelFibers.Count - errorCounter) / (double)expectedSteelFibers.Count * 100.0;
                var percentageString = percentage.ToString("0.##");     // 2 tizedesjegyre kerekítés
                results += $"Pontosság: {percentageString}%\n";
            }
            else
            {
                results += $"Acélszálak várt száma: {expectedSteelFibers.Count}\n";
                results += $"Acélszálak valós száma: {actualSteelFibers.Count}\n";
                foreach (var fiber in actualSteelFibers)
                {
                    if (!SteelFiberCorrect(fiber))
                    {
                        results += $"Hibás acélszál: {fiber.ToString()}\n";
                        errorCounter++;
                    }
                }
                results += $"Hibák száma: {errorCounter}\n";
                var percentage = (double)(actualSteelFibers.Count - errorCounter) / (double)actualSteelFibers.Count * 100.0;
                var percentageString = percentage.ToString("0.##");
                results += $"Pontosság: {percentageString}%\n";

            }
            return results;
        }

        private bool BlobsAreCorrect(List<SteelFiber> expected, SteelFiber actual)
        {
            var fiber = expected.FirstOrDefault(f => actual.Blobs.All(b => f.Blobs.Contains(b)) && f.Blobs.All(b => actual.Blobs.Contains(b)));

            return fiber != null;
        }
        private bool SteelFiberCorrect(SteelFiber actual)
        {
            var temp = expectedSteelFibers.FirstOrDefault(sf => 
                sf.Blobs.All(b => actual.Blobs.Contains(b)) && 
                actual.Blobs.All(b => sf.Blobs.Contains(b)));
            return temp != null;
        }
        private bool IdIsCorrect(SteelFiber expected, SteelFiber actual)
        {
            return expected.SteelFiberId == actual.SteelFiberId;
        }
    }
}
