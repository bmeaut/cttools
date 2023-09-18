using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Cv4s.Operations.OperationTools
{
    public static class PlotHelper
    {
        /// <summary>
        /// Creates the ColumnPlot with the given <paramref name="title"/>, <paramref name="xlabel"/>,<paramref name="ylabel"/>,<paramref name="x"/> data, <paramref name="y"/> data
        /// </summary>
        /// <param name="title"> The title of the diagram</param>
        /// <param name="xlabel">the x Axis label</param>
        /// <param name="ylabel">the y Axis label</param>
        /// <param name="x"> column types</param>
        /// <param name="y"> column values</param>
        /// <returns> the freshly created ColumnPlot </returns>
        public static PlotModel CreateColumnPlot(
            string title, string xlabel, string ylabel, double[] x, double[] y)
        {
            var percentages = new List<ColumnItem>();

            for (int i = 0; i < x.Length; i++)
            {
                percentages.Add(new ColumnItem
                {
                    Value = y[i],
                });
            }

            var Series = new OxyPlot.Series.ColumnSeries
            {
                ItemsSource = percentages,
                LabelPlacement = LabelPlacement.Outside,
                LabelFormatString = "{0:.00}%"
            };

            var Axes_x = new OxyPlot.Axes.CategoryAxis
            {
                Position = AxisPosition.Bottom,
                Title = xlabel,
                ItemsSource = x,
            };

            return AddValueToModel(title, Axes_x, null, Series);
        }

        public static PlotModel CreateColumnPlot(
            string title, string xlabel, string ylabel, int[] x, int[] y)
        {
            double[] xd = x.Select(i => (double)i).ToArray();
            double[] yd = y.Select(i => (double)i).ToArray();

            return CreateColumnPlot(title, xlabel, ylabel, xd, yd);
        }

        public static PlotModel CreateScatterPlot(
            string title, string xlabel, string ylabel, int[] x, int[] y)
        {
            double[] xd = x.Select(i => (double)i).ToArray();
            double[] yd = y.Select(i => (double)i).ToArray();

            return CreateScatterPlot(title, xlabel, ylabel, xd, yd);
        }

            public static PlotModel CreateScatterPlot(
             string title, string xlabel, string ylabel, double[] x, double[] y)
        {
            var points = new List<ScatterPoint>();
            for (int i = 0; i < x.Length; i++)
                points.Add(new ScatterPoint(x[i], y[i]));

            var Series = new OxyPlot.Series.ScatterSeries()
            {
                ItemsSource = points
            };
            var Axes_x = new OxyPlot.Axes.LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = xlabel
            };
            var Axes_y = new OxyPlot.Axes.LinearAxis
            {
                Position = AxisPosition.Left,
                Title = ylabel
            };

            return AddValueToModel(title, Axes_x, Axes_y, Series);
        }


        private static PlotModel AddValueToModel(string Title, Axis? AxisX, Axis? AxisY, Series Series)
        {
            var model = new OxyPlot.PlotModel() { Title = Title };
            model.Series.Add(Series);

            if (AxisX != null)
                model.Axes.Add(AxisX);

            if (AxisY != null)
                model.Axes.Add(AxisY);

            return model;
        }
    }
}
