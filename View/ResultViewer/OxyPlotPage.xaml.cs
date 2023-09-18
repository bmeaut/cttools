using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Core.Operation;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;

namespace View
{
    /// <summary>
    /// Interaction logic for OxyPlotPage.xaml
    /// </summary>
    public partial class OxyPlotPage : Page
    {
        private OxyPlotPageViewModel viewModel;
        public OxyPlotPage(PlotModel plotModel)
        {
            InitializeComponent();
            viewModel = new OxyPlotPageViewModel();
            DataContext = viewModel;
            // GC needs to be called because oxyplot cant attach plotmodels that used in an other plotView
            // For more info see issue https://github.com/oxyplot/oxyplot/issues/497 
            GC.Collect();
            GC.WaitForPendingFinalizers();
            viewModel.PlotModel = plotModel;
        }


        public string DiagramTitle { get; private set; }

        public IList<DataPoint> Points { get; private set; }
    }

    public class OxyPlotPageViewModel : INotifyPropertyChanged
    {
        public static OxyplotInternalOutput CreateScatterPlot(
            string title, string xlabel, string ylabel, int[] x, int[] y)
        {
            var points = new List<ScatterPoint>();
            for (int i = 0; i < x.Length; i++)
                points.Add(new ScatterPoint(x[i], y[i]));

            var model = new OxyPlot.PlotModel() { Title = title };
            model.Series.Add(new OxyPlot.Series.ScatterSeries()
            {
                ItemsSource = points
            });
            model.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = xlabel
            });
            model.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = AxisPosition.Left,
                Title = ylabel
            });

            return new OxyplotInternalOutput() { PlotModel = model }; ;
        }
        private PlotModel plotModel;
        public PlotModel PlotModel
        {
            get { return plotModel; }
            set { plotModel = value; OnPropertyChanged("PlotModel"); }
        }

        public OxyPlotPageViewModel()
        {
            //var x = new int[] { 1, 3, 5, 7, 9 };
            //plotModel = OxyplotInternalOutput.CreateScatterPlot("Title2", "X", "Y", x, x).PlotModel;
        }

        public event PropertyChangedEventHandler PropertyChanged;


        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
