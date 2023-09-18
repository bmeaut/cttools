using Core.Interfaces.Operation;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System.Collections.Generic;
using System.Linq;

namespace Core.Operation
{
    /// <summary>
    /// OxyplotInternalOutput Serialization is working as it serializes primitives and builds up from them again in getter method.
    /// This is mandatory because PlotModel objects cannot be serialized easily.
    /// If converters for these classes will exist in the future, this wrapping can be simplified.
    /// 
    ///- Example for creating a scatter plot and exporting to png:
    /// var output = OxyplotInternalOutput.CreateScatterPlot(
    ///"title", "xlabel", "ylabel",
    ///new int[] { 1, 2, 5, 6, 7 },
    ///new int[] { 10, 20, 30, 40, 40 }
    ///);
    ///output.ExportToPngFile("oxyplot_export.png");

    /// - Visualization via UI:
    /// UI: PlotView, PlotView.Model = OxyplotInternalOutput.PlotModel
    /// </summary>
    public class OxyplotInternalOutput : InternalOutput
    {
        protected PlotModel _model;

        [Newtonsoft.Json.JsonIgnore]
        public OxyPlot.Series.Series Series { get; protected set; }
        [Newtonsoft.Json.JsonIgnore]
        public OxyPlot.Axes.Axis Axes_x { get; protected set; }
        [Newtonsoft.Json.JsonIgnore]
        public OxyPlot.Axes.Axis Axes_y { get; protected set; }


        public string Title { get; set; }
        public string XLabel { get; set; }
        public string YLabel { get; set; }
        public double[] X { get; set; }
        public double[] Y { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public virtual PlotModel PlotModel
        {
            get
            {
                if (_model == null)
                {
                    var points = new List<ScatterPoint>();
                    for (int i = 0; i < X.Length; i++)
                        points.Add(new ScatterPoint(X[i], Y[i]));
                    Series = new OxyPlot.Series.ScatterSeries()
                    {
                        ItemsSource = points
                    };
                    Axes_x = new OxyPlot.Axes.LinearAxis
                    {
                        Position = AxisPosition.Bottom,
                        Title = XLabel
                    };
                    Axes_y = new OxyPlot.Axes.LinearAxis
                    {
                        Position = AxisPosition.Left,
                        Title = YLabel
                    };

                    AddValueToModel();
                }
                return _model;
            }
            set
            {
                Series = value.Series[0];
                Axes_x = (OxyPlot.Axes.LinearAxis)value.Axes[0];
                Axes_y = (OxyPlot.Axes.LinearAxis)value.Axes[1];
            }
        }

        protected void AddValueToModel()
        {
            _model = new OxyPlot.PlotModel() { Title = Title };
            _model.Series.Add(Series);

            if(Axes_x != null)
            _model.Axes.Add(Axes_x);

            if(Axes_y !=null)
                _model.Axes.Add(Axes_y);
        }

        /// <summary>
        /// Convenience Factory Method - The PlotModel instance is only created when PlotModel is asked for.
        /// In the future consider PlotModel creation in the UI model and OxyplotInternalOutput be a dataclass only
        /// This will remove Oxyplot dependency from the Core project.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="xlabel"></param>
        /// <param name="ylabel"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static OxyplotInternalOutput CreateScatterPlot(
            string title, string xlabel, string ylabel, int[] x, int[] y)
        {
            double[] xd = x.Select(i => (double)i).ToArray();
            double[] yd = y.Select(i => (double)i).ToArray();
            var oxyplotInternalOutput = new OxyplotInternalOutput
            {
                Title = title,
                X = xd,
                Y = yd,
                XLabel = xlabel,
                YLabel = ylabel
            };
            return oxyplotInternalOutput;
        }

        public static OxyplotInternalOutput CreateScatterPlot(
            string title, string xlabel, string ylabel, double[] x, double[] y)
        {
            var oxyplotInternalOutput = new OxyplotInternalOutput
            {
                Title = title,
                X = x,
                Y = y,
                XLabel = xlabel,
                YLabel = ylabel
            };
            return oxyplotInternalOutput;
        }

        public void ExportToPngFile(string filename)
        {
            var pngExporter = new PngExporter()
            { Width = 600, Height = 400, Background = OxyColors.White };
            pngExporter.ExportToFile(PlotModel, filename);
        }
    }
}
