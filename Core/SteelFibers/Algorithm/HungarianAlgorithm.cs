using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.SteelFibers.Algorithm
{
    // A magyar módszer implementációja
    // visszaadja az optimális párosítássr a súlymátrixban
    public class HungarianAlgorithm : IAlgorithm
    {
        // visszaadja az optimális blobID összerendelést
        public Point[] Solve(double[,] originalWeights) // https://en.wikipedia.org/wiki/Hungarian_algorithm#Matrix_interpretation
        {
            List<int> blobsInLastLayerWithNoPairs = FindEndingFibers(originalWeights);
            double[,] weights = removeBlobsWithoutPairs(originalWeights, blobsInLastLayerWithNoPairs);

            int originalHeight = weights.GetLength(0);
            int originalWidth = weights.GetLength(1);
            weights = makeMatrixSquare(weights);
            int height = weights.GetLength(0);
            int width = weights.GetLength(1);

            weights = subtractSmallestWeightFromRowsAndColumns(weights, height, width);

            bool done = false;
            while (!done)
            {
                List<int>[] rowsAndColumnWithLines = coverAllZerosUsingMinimalRowsAndCols(height, width, weights);

                if (rowsAndColumnWithLines[0].Count + rowsAndColumnWithLines[1].Count != Math.Min(weights.GetLength(0), weights.GetLength(1)))
                {
                    weights = subtractMinValueFromUnmarkedRowsAndAddToMarkedCols(height, width, weights, rowsAndColumnWithLines);
                }
                else
                {
                    done = true;
                }
            }
            Point[] solutonsInReducedWeightMatrix = findZeroInEveryRow(originalHeight, originalWidth, weights);
            return solutionWithRemovedRows(solutonsInReducedWeightMatrix, blobsInLastLayerWithNoPairs);
        }

        private Point[] solutionWithRemovedRows(Point[] solutonsInReducedWeightMatrix, List<int> blobsInLastLayerWithNoPairs)
        {
            for (int i = 0; i < solutonsInReducedWeightMatrix.Length; i++)
            {
                for (int j = 0; j < blobsInLastLayerWithNoPairs.Count; j++)
                {
                    if (solutonsInReducedWeightMatrix[i].X >= blobsInLastLayerWithNoPairs[j])
                    {
                        solutonsInReducedWeightMatrix[i].X++;
                    }
                }
            }
            return solutonsInReducedWeightMatrix;
        }

        private double[,] removeBlobsWithoutPairs(double[,] weights, List<int> blobsInLastLayerWithNoPairs)
        {
            int originalHeight = weights.GetLength(0);
            int originalWidth = weights.GetLength(1);

            double[,] removed = new double[originalHeight - blobsInLastLayerWithNoPairs.Count, originalWidth];

            int height = removed.GetLength(0);
            int width = removed.GetLength(1);

            int counter = 0;
            for (int i = 0; i < height; i++)
            {
                int row = i + counter;
                while (blobsInLastLayerWithNoPairs.Contains(row))
                {
                    counter++;
                    row++;
                }
                for (int j = 0; j < width; j++)
                {
                    removed[i, j] = weights[row, j];
                }
            }
            return removed;
        }

        private List<int> FindEndingFibers(double[,] weights)
        {
            int height = weights.GetLength(0);
            int width = weights.GetLength(1);
            List<int> endingBlobs = new List<int>();
            for (int i = 0; i < height; i++)
            {
                double min = int.MaxValue;
                for (int j = 0; j < width; j++)
                {
                    if (min > weights[i, j])
                    {
                        min = weights[i, j];
                    }
                }
                if (min >= 60)
                {
                    endingBlobs.Add(i);
                }
            }
            return endingBlobs;
        }

        private double[,] makeMatrixSquare(double[,] weights)
        {
            int height = weights.GetLength(0);
            int width = weights.GetLength(1);
            int difference = height - width;

            double[,] squareWeights = new double[height, height];
            if (width > height)
            {
                squareWeights = new double[width, width];
            }

            if (difference > 0)         // height > width, több oszlop kell
            {
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        squareWeights[i, j] = weights[i, j];
                    }
                    for (int j = 0; j < difference; j++)
                    {
                        squareWeights[i, width + j] = 0;
                    }
                }
            }
            else if (difference < 0)  // height < width, több sor kell
            {
                for (int j = 0; j < width; j++)
                {
                    for (int i = 0; i < height; i++)
                    {
                        squareWeights[i, j] = weights[i, j];
                    }
                    for (int i = 0; i < -difference; i++)
                    {
                        squareWeights[height + i, j] = 0;
                    }
                }
            }
            else
            {
                squareWeights = weights;    // height == width
            }
            return squareWeights;
        }

        // első és második lépés
        private double[,] subtractSmallestWeightFromRowsAndColumns(double[,] weights, int height, int width)
        {
            double min;
            for (int i = 0; i < height; i++)
            {
                min = int.MaxValue;
                for (int j = 0; j < width; j++)
                {
                    if (min > weights[i, j])
                    {
                        min = weights[i, j];
                    }
                }
                for (int j = 0; j < width; j++)
                {
                    weights[i, j] -= min;
                }
            }

            for (int i = 0; i < width; i++)
            {
                min = int.MaxValue;
                for (int j = 0; j < height; j++)
                {
                    if (min > weights[j, i])
                    {
                        min = weights[j, i];
                    }
                }
                for (int j = 0; j < height; j++)
                {
                    weights[j, i] -= min;
                }
            }
            return weights;
        }

        // harmadik lépés
        private List<int>[] coverAllZerosUsingMinimalRowsAndCols(int height, int width, double[,] weights)
        {
            int[] assignmentsInRows = findBestAssignmentOfZeros(height, width, weights);
            HashSet<int> markedRows = new HashSet<int>();
            HashSet<int> markedColumns = new HashSet<int>();

            for (int i = 0; i < height; i++)
            {
                if (assignmentsInRows[i] == -1)
                {
                    markedRows.Add(i);
                }
            }

            HashSet<int> newlyMarkedRows = new HashSet<int>();
            HashSet<int> newlyMarkedColumns = new HashSet<int>();
            foreach (int item in markedRows)
            {
                newlyMarkedRows.Add(item);
            }

            bool changed = true;
            while (changed)
            {
                changed = false;
                newlyMarkedColumns = new HashSet<int>();
                for (int j = 0; j < width; j++)
                {
                    for (int i = 0; i < height; i++)
                    {
                        if (weights[i, j] == 0 && newlyMarkedRows.Contains(i) && !markedColumns.Contains(j) && !newlyMarkedColumns.Contains(j))
                        {
                            newlyMarkedColumns.Add(j);
                            changed = true;
                        }
                    }
                }
                markedColumns.UnionWith(newlyMarkedColumns);
                newlyMarkedRows = new HashSet<int>();
                for (int i = 0; i < height; i++)
                {
                    int assignment = assignmentsInRows[i];
                    if (assignment != -1 && newlyMarkedColumns.Contains(assignment) && !markedRows.Contains(i))
                    {
                        newlyMarkedRows.Add(i);
                        changed = true;
                    }
                }
                markedRows.UnionWith(newlyMarkedRows);
            }
            int numberOfLinesTotal = 0;
            List<int> rowsWithLine = new List<int>();
            List<int> columnsWithLine = new List<int>();

            foreach (int column in markedColumns)
            {
                columnsWithLine.Add(column);
                numberOfLinesTotal++;
            }
            for (int i = 0; i < height; i++)
            {
                if (!markedRows.Contains(i))
                {
                    rowsWithLine.Add(i);
                    numberOfLinesTotal++;
                }
            }

            List<int>[] rowsAndColumnWithLines = new List<int>[2];
            rowsAndColumnWithLines[0] = rowsWithLine;
            rowsAndColumnWithLines[1] = columnsWithLine;
            return rowsAndColumnWithLines;
        }

        private int[] findBestAssignmentOfZeros(int height, int width, double[,] weights)
        {
            int[] assignmentsInRows = new int[height];  // i-edik sorban -> az érték az assignmentsInRows[i] oszlopban hozzárendelve ("assigend")
            for (int i = 0; i < height; i++)
            {
                assignmentsInRows[i] = -1;
            }

            bool[] crossedRow = new bool[height];
            bool[] crossedColumn = new bool[width];
            bool changed = true;
            while (changed)
            {
                changed = false;

                for (int i = 0; i < height; i++)
                {
                    int countOfZeros = 0;
                    int pos = -1;
                    for (int j = 0; j < width; j++)
                    {
                        if (weights[i, j] == 0 && !crossedRow[i] && !crossedColumn[j])
                        {
                            countOfZeros++;
                            pos = j;
                        }
                    }
                    if (countOfZeros == 1)
                    {
                        if (weights[i, pos] == 0 && !crossedRow[i] && !crossedColumn[pos])
                        {
                            crossedRow[i] = true;
                            crossedColumn[pos] = true;
                            changed = true;
                            assignmentsInRows[i] = pos;
                        }
                    }
                }

                for (int j = 0; j < width; j++)
                {
                    int countOfZeros = 0;
                    int pos = -1;
                    for (int i = 0; i < height; i++)
                    {
                        if (weights[i, j] == 0 && !crossedRow[i] && !crossedColumn[j])
                        {
                            countOfZeros++;
                            pos = i;
                        }
                    }
                    if (countOfZeros == 1)
                    {
                        if (weights[pos, j] == 0 && !crossedRow[pos] && !crossedColumn[j])
                        {
                            crossedRow[pos] = true;
                            crossedColumn[j] = true;
                            changed = true;
                            assignmentsInRows[pos] = j;
                        }
                    }
                }

                if (!changed)
                {
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            if (weights[i, j] == 0 && !crossedColumn[j] && !crossedRow[i])
                            {
                                crossedRow[i] = true;
                                crossedColumn[j] = true;
                                changed = true;
                                assignmentsInRows[i] = j;
                            }
                        }
                    }
                }
            }
            return assignmentsInRows;
        }

        // negyedik lépés
        private double[,] subtractMinValueFromUnmarkedRowsAndAddToMarkedCols(int height, int width, double[,] weights, List<int>[] rowsAndColumnWithLines)
        {
            double min = int.MaxValue;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    if (!rowsAndColumnWithLines[0].Contains(i) && !rowsAndColumnWithLines[1].Contains(j))
                    {
                        if (min > weights[i, j])
                        {
                            min = weights[i, j];
                        }
                    }
                }
            }

            for (int i = 0; i < height; i++)
            {
                if (!rowsAndColumnWithLines[0].Contains(i))
                {
                    for (int j = 0; j < width; j++)
                    {
                        weights[i, j] -= min;
                    }
                }
            }

            for (int j = 0; j < width; j++)
            {
                if (rowsAndColumnWithLines[1].Contains(j))
                {
                    for (int i = 0; i < height; i++)
                    {
                        weights[i, j] += min;
                    }
                }
            }
            return weights;
        }

        private Point[] findZeroInEveryRow(int originalHeight, int originalWidth, double[,] weights)
        {
            int height = weights.GetLength(0);
            int width = weights.GetLength(1);
            List<int>[] posOfZeros = new List<int>[height];
            for (int i = 0; i < height; i++)
            {
                posOfZeros[i] = new List<int>();
                for (int j = 0; j < width; j++)
                {
                    if (weights[i, j] == 0)
                    {
                        posOfZeros[i].Add(j);
                    }
                }
            }

            int[] zerosTestedInRows = new int[height];
            int[] zeroChosenInRows = new int[height];
            bool found = false;
            int counter = 0;
            List<Point> checkList = new List<Point>();
            while (!found)
            {
                counter = 0;
                zeroChosenInRows = new int[height];
                for (int i = 0; i < height; i++)
                {
                    zeroChosenInRows[i] = posOfZeros[i].ElementAt(zerosTestedInRows[i]);
                }

                checkList = new List<Point>();
                for (int i = 0; i < zeroChosenInRows.Length; i++)
                {
                    bool contained = doesCheckListContainColumn(checkList, zeroChosenInRows[i]);
                    if (contained && counter == 0)
                    {
                        counter++;
                    }
                    else
                    {
                        if (!contained)
                        {
                            if ((i < originalHeight && zeroChosenInRows[i] < originalWidth))
                            {
                                checkList.Add(new Point(i, zeroChosenInRows[i]));
                            }
                        }
                    }
                }
                HashSet<Point> countOfSolution = new HashSet<Point>();
                for (int i = 0; i < checkList.Count; i++)
                {
                    countOfSolution.Add(checkList[i]);
                }
                int minSize = Math.Min(originalHeight, originalWidth);
                if ((checkList.Count == minSize && counter == 0) || (countOfSolution.Count == minSize))
                {
                    found = true;
                }
                else
                {
                    zerosTestedInRows = stepCombination(zerosTestedInRows, posOfZeros, 0);
                }
            }

            Point[] solution = new Point[checkList.Count];
            for (int i = 0; i < checkList.Count; i++)
            {
                solution[i] = checkList.ElementAt(i);
            }
            return solution;
        }

        private bool doesCheckListContainColumn(List<Point> checkList, int col)
        {
            bool contained = false;
            for (int i = 0; i < checkList.Count; i++)
            {
                if (checkList.ElementAt(i).Y == col)
                {
                    contained = true;
                }
            }
            return contained;
        }

        private int[] stepCombination(int[] zerosTestedInRows, List<int>[] posOfZeros, int row)
        {
            if (zerosTestedInRows[row] < posOfZeros[row].Count - 1)
            {
                zerosTestedInRows[row]++;
            }
            else
            {
                zerosTestedInRows[row] = 0;
                if (row < zerosTestedInRows.Length - 1)
                {
                    stepCombination(zerosTestedInRows, posOfZeros, row + 1);
                }
            }
            return zerosTestedInRows;
        }
    }
}
