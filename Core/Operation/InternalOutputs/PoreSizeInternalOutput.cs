using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Operation.InternalOutputs
{
    /// <summary>
    /// InternalOutput class for showing the <see cref="PoreSizeStatOperation"/> result as a column plot
    /// </summary>
    public class PoreSizeInternalOutput : OxyplotInternalOutput
    {
        /// <summary>
        /// Creates the columnPlot from the previously given datas
        /// </summary>
        public override PlotModel PlotModel { get {
                if (_model == null)
                {
                    var percentages = new List<ColumnItem>();

                    for (int i = 0; i < X.Length; i++)
                    {
                        percentages.Add(new ColumnItem
                        {
                            Value = Y[i],
                        });
                    }

                    Series = new OxyPlot.Series.ColumnSeries
                    {
                        ItemsSource = percentages,
                        LabelPlacement = LabelPlacement.Outside,
                        LabelFormatString = "{0:.00}%"
                    };

                    Axes_x = new OxyPlot.Axes.CategoryAxis
                    {
                        Position = AxisPosition.Bottom,
                        Title = XLabel,
                        ItemsSource = X,
                    };

                    base.AddValueToModel();
                }
                return _model;
            } set => base.PlotModel = value; 
        }

        /// <summary>
        /// Creates the ColumnPlot with the given <paramref name="title"/>, <paramref name="xlabel"/>,<paramref name="ylabel"/>,<paramref name="x"/> data, <paramref name="y"/> data
        /// </summary>
        /// <param name="title"> The title of the diagram</param>
        /// <param name="xlabel">the x Axis label</param>
        /// <param name="ylabel">the y Axis label</param>
        /// <param name="x"> column types</param>
        /// <param name="y"> column values</param>
        /// <returns> the freshly created ColumnPlot Internal Output</returns>
        public static PoreSizeInternalOutput CreateColumnPlot(
            string title, string xlabel, string ylabel, double[] x, double[] y)
        {
            return new PoreSizeInternalOutput
            {
                Title = title,
                X = x,
                Y = y,
                XLabel = xlabel,
                YLabel = ylabel,
            };
        }
    }
}
