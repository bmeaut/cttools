using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Operation.InternalOutputs
{
    public class PoreDistributionInternalOutput:InternalOutput
    {
        private PlotModel _model;

        public OxyPlot.Series.Series Series { get; private set; }
        public OxyPlot.Axes.LogarithmicAxis Axes_x { get; private set; }
        public OxyPlot.Axes.LinearAxis Axes_y { get; private set; }

        public string Title { get; set; }
        public string XLabel { get; set; }
        public string YLabel { get; set; }
        public double[] X { get; set; }
        public double[] Y { get; set; }

        //A határérték görbe
        public Dictionary<double, double> LimitCurveA = new Dictionary<double, double>();
        //B határérték görbe
        public Dictionary<double, double> LimitCurveB = new Dictionary<double, double>();
        //C határérték görbe
        public Dictionary<double, double> LimitCurveC = new Dictionary<double, double>();


        private List<DataPoint> LimitCurveAPoints = new List<DataPoint>();
        private List<DataPoint> LimitCurveBPoints = new List<DataPoint>();
        private List<DataPoint> LimitCurveCPoints = new List<DataPoint>();
        private OxyColor LimitCurveColor = OxyColor.FromRgb(255, 0, 0);
        private OxyColor DistributionColor = OxyColor.FromRgb(0, 0, 0);
        private double LimitLineThickness = 2;
        private double DistributionLineThickness = 2;

        private string LimitCurveATitle = "A32";
        private string LimitCurveBTitle = "B32";
        private string LimitCurveCTitle = "C32";

        private double MaximumXAxisValue = 64;
        private double MinimumXAxisValue = 0.0625;
        private double MaximumYAxisValue = 100;
        private double YAxisScale = 10;

        [Newtonsoft.Json.JsonIgnore]
        public PlotModel PlotModel {
            get
            {
                if (_model == null)
                {
                    if (_model != null) {
                        _model.Series.Clear();
                        _model.Axes.Clear();
                    }

                    foreach (var point in LimitCurveA)
                    {
                        LimitCurveAPoints.Add(new DataPoint(point.Key, point.Value));
                    }

                    foreach (var point in LimitCurveB)
                    {
                        LimitCurveBPoints.Add(new DataPoint(point.Key, point.Value));
                    }

                    foreach (var point in LimitCurveC)
                    {
                        LimitCurveCPoints.Add(new DataPoint(point.Key, point.Value));
                    }

                    var limitCurveASeries = new OxyPlot.Series.LineSeries()
                    {
                        Title = LimitCurveATitle,
                        Color = LimitCurveColor,
                        StrokeThickness = LimitLineThickness,
                        ItemsSource = LimitCurveAPoints
                    };

                    var limitCurveBSeries = new OxyPlot.Series.LineSeries()
                    {
                        Title = LimitCurveBTitle,
                        Color = LimitCurveColor,
                        StrokeThickness = LimitLineThickness,
                        ItemsSource = LimitCurveBPoints
                    };

                    var limitCurveCSeries = new OxyPlot.Series.LineSeries()
                    {
                        Title = LimitCurveCTitle,
                        Color = LimitCurveColor,
                        StrokeThickness = LimitLineThickness,
                        ItemsSource = LimitCurveCPoints
                    };

                    var DistributionPoints = new List<DataPoint>();
                   for (int i = 0; i < X.Length; i++)
                        DistributionPoints.Add(new DataPoint(X[i], Y[i]));

                   Series = new OxyPlot.Series.LineSeries()
                   {
                            StrokeThickness = DistributionLineThickness,
                            Color = DistributionColor,
                            ItemsSource = DistributionPoints
                   };

                    Axes_x = new OxyPlot.Axes.LogarithmicAxis
                    {
                        Position = AxisPosition.Bottom,
                        Title = XLabel,
                        Maximum = MaximumXAxisValue,
                        Minimum = MinimumXAxisValue,
                    };

                    Axes_y = new OxyPlot.Axes.LinearAxis
                    {
                        Position = AxisPosition.Left,
                        Title = YLabel,
                        Maximum = MaximumYAxisValue,
                        MajorStep = YAxisScale
                    };

                    _model = new OxyPlot.PlotModel()
                    {
                        Title = Title
                    };

                    _model.Series.Add(limitCurveASeries);
                    _model.Series.Add(limitCurveBSeries);
                    _model.Series.Add(limitCurveCSeries);
                    _model.Series.Add(Series);

                    _model.Axes.Add(Axes_x);
                    _model.Axes.Add(Axes_y);
                    
                }
                return _model;
            }
            set
            {
                Series = value.Series[0];
                Axes_x = (OxyPlot.Axes.LogarithmicAxis)value.Axes[0];
                Axes_y = (OxyPlot.Axes.LinearAxis)value.Axes[1];
            }
        }

        public static PoreDistributionInternalOutput CreateDistribution(
            string title, string xlabel, string ylabel, double[] x, double[] y, double Dmax)
        {
            var limitCurves = LimitCurveHelper.GetLimitCurves(Dmax);

            var distributionInternalOutput = new PoreDistributionInternalOutput
            {
                Title = title,
                X = x,
                Y = y,
                XLabel = xlabel,
                YLabel = ylabel,
                LimitCurveA = limitCurves[0],
                LimitCurveB = limitCurves[1],
                LimitCurveC = limitCurves[2]
            };

            return distributionInternalOutput;
        }

    }

    public class LimitCurveHelper
    {
        //Dmax ==> 1mm
        //A határérték görbe
        private static Dictionary<double, double> LimitCurveA1mm = new Dictionary<double, double>() {{0.0625,0},{0.125,0},{0.25,10},{0.5,40 },{1,90},{2,100 }};
        //B határérték görbe
        private static Dictionary<double, double> LimitCurveB1mm = new Dictionary<double, double>() { { 0.125, 8 }, { 0.25, 25 }, { 0.5, 64 } };
        //C határérték görbe
        private static Dictionary<double, double> LimitCurveC1mm = new Dictionary<double, double>(){{0.0625,6},{0.125,18},{0.25,43},{0.5,78},{1,100},{2,100} };

        //Dmax ==> 2mm
        //A határérték görbe
        private static Dictionary<double, double> LimitCurveA2mm = new Dictionary<double, double>() {{0.0625,0},{0.125,0},{0.25,13 },{0.5,27 },{1,50},{2,90 },{4, 100}};
        //B határérték görbe
        private static Dictionary<double, double> LimitCurveB2mm = new Dictionary<double, double>(){{0.125,6},{0.25,23},{0.5,44},{1,64}};
        //C határérték görbe
        private static Dictionary<double, double> LimitCurveC2mm = new Dictionary<double, double>(){{0.0625,5},{0.125,13},{0.25,32},{0.5,53},{1,72},{2,100},{4,100}};

        //Dmax ==> 4mm
        //A határérték görbe
        private static Dictionary<double, double> LimitCurveA4mm = new Dictionary<double, double>(){
            {0.0625,0},{0.125,0},{0.25,8 },{0.5,18 },{1,34},{2,60 },{4, 90},{8, 100} };
        //B határérték görbe
        private  static Dictionary<double, double> LimitCurveB4mm = new Dictionary<double, double>(){
            {0.125,5},{0.25,16},{0.5,33},{1,50},{2,74}};
        //C határérték görbe
        private static Dictionary<double, double> LimitCurveC4mm = new Dictionary<double, double>(){
            {0.0625,4},{0.125,12},{0.25,26},{0.5,44},{1,62},{2,82},{4,100},{8,100}};

        //Dmax ==> 8mm
        //A határérték görbe
        private static Dictionary<double, double> LimitCurveA8mm = new Dictionary<double, double>() {
            {0.0625,0},{0.125,0},{0.25,5 },{0.5,11 },{1,21},
            {2,37 },{4, 61},{8, 95},{12,100}};
        //B határérték görbe
        private static Dictionary<double, double> LimitCurveB8mm = new Dictionary<double, double>(){
            {0.125,5},{0.25,13},{0.5,25},{1,38},
            {2,55},{4,75}};
        //C határérték görbe
        private static Dictionary<double, double> LimitCurveC8mm = new Dictionary<double, double>(){
            {0.0625,3},{0.125,8},{0.25,20},{0.5,36},{1,52},
            {2,67},{4,84},{8,100},{12,100}};

        //Dmax ==> 16mm
        //A határérték görbe
        private static  Dictionary<double, double> LimitCurveA16mm = new Dictionary<double, double>() {
            {0.0625,0},{0.125,0},{0.25,3 },{0.5,6 },{1,14},
            {2,24 },{4,37},{8, 61},{16,95}, {24,100} };
        //B határérték görbe
        private static Dictionary<double, double> LimitCurveB16mm = new Dictionary<double, double>(){
            {0.125,4},{0.25,9},{0.5,19},{1,29},
            {2,43},{4,59},{8,78}};
        //C határérték görbe
        private static Dictionary<double, double> LimitCurveC16mm = new Dictionary<double, double>(){
            {0.0625,3},{0.125,9},{0.25,18},{0.5,31},{1,44},
            {2,58},{4,71},{8,86},{16,100},{24,100} };

        public static Dictionary<double,double>[] GetLimitCurves(double Dmax)
        {
            Dictionary<double, double>[] limitCurves = new Dictionary<double, double>[3];
            switch (Dmax)
            {
                case 1:
                    limitCurves[0] = LimitCurveA1mm;
                    limitCurves[1] = LimitCurveB1mm;
                    limitCurves[2] = LimitCurveC1mm;
                    break;
                case 2:
                    limitCurves[0] = LimitCurveA2mm;
                    limitCurves[1] = LimitCurveB2mm;
                    limitCurves[2] = LimitCurveC2mm;
                    break;
                case 4:
                    limitCurves[0] = LimitCurveA4mm;
                    limitCurves[1] = LimitCurveB4mm;
                    limitCurves[2] = LimitCurveC4mm;
                    break;
                case 8:
                    limitCurves[0] = LimitCurveA8mm;
                    limitCurves[1] = LimitCurveB8mm;
                    limitCurves[2] = LimitCurveC8mm;
                    break;
                case 16:
                    limitCurves[0] = LimitCurveA16mm;
                    limitCurves[1] = LimitCurveB16mm;
                    limitCurves[2] = LimitCurveC16mm;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Dmax size is out of range");
            }
            return limitCurves;
        }
    }
}
