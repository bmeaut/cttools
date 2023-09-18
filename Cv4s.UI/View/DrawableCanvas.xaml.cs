using Cv4s.Common.Models.Images;
using Cv4s.UI.Converters;
using Cv4s.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cv4s.UI.View
{
    /// <summary>
    /// Interaction logic for DrawableCanvas.xaml
    /// </summary>
    public partial class DrawableCanvas : UserControl
    {
        public MeasurementEditorViewModel MeasurementEditorViewModel;

        private bool _isZoomKeyPressed = false;
        private double zoomLevel = 1.0;
        private System.Windows.Point iniP;

        public DrawableCanvas()
        {
            InitializeComponent();
            SetInkCanvasDrawingAttributes();
            Focusable = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if(e.Key == Key.LeftCtrl)
            {
                _isZoomKeyPressed = true;
            }

            if (e.Key == Key.D0) MeasurementEditorViewModel.CurrentAppearance = 0;//Raw
            if (e.Key == Key.D1) MeasurementEditorViewModel.CurrentAppearance = 1;//0
            if (e.Key == Key.D2) MeasurementEditorViewModel.CurrentAppearance = 2;//1
            if (e.Key == Key.D3) MeasurementEditorViewModel.CurrentAppearance = 3;//2
            if (e.Key == Key.D4) MeasurementEditorViewModel.CurrentAppearance = 4;//3

            if (MeasurementEditorViewModel.IsOperationCallableFromCanvas)
            {
                if (e.Key == Key.E) MeasurementEditorViewModel.DrawStyle = DrawStyle.Ellipsis;
                if (e.Key == Key.R) MeasurementEditorViewModel.DrawStyle = DrawStyle.Rectangle;
                if (e.Key == Key.I) MeasurementEditorViewModel.DrawStyle = DrawStyle.Ink;
            }
            else
            {
                MeasurementEditorViewModel.DrawStyle = DrawStyle.None;
            }

        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if(e.Key == Key.LeftCtrl)
            {
                _isZoomKeyPressed = false;
            }
        }

        public Brush InkCanvasBackground
        {
            get
            {
                return Canvas.Background;
            }

            set
            {
                Canvas.Background = value;
            }
        }

        private void SetInkCanvasDrawingAttributes()
        {
            var inkDA = new DrawingAttributes
            {
                Color = Colors.SpringGreen,
                Height = 3,
                Width = 3,
                FitToCurve = false,
                StylusTip = StylusTip.Ellipse,
                IsHighlighter = false,
                IgnorePressure = true
            };

            Canvas.EditingMode = InkCanvasEditingMode.None;
            Canvas.DefaultDrawingAttributes = inkDA;
        }

        private void InkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && MeasurementEditorViewModel.IsOperationCallableFromCanvas)
                iniP = e.GetPosition(Canvas);
        }

        private void InkCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_isZoomKeyPressed)
            {
                zoomLevel += (double)e.Delta / 1500.0;
                InkCanvasScaleTransform.ScaleX = zoomLevel;
                InkCanvasScaleTransform.ScaleY = zoomLevel;
            }
            else
            {
                int currentLayer = MeasurementEditorViewModel.SelectedLayer;
                int nextLayer = currentLayer + (e.Delta < 0 ? -1 : 1);
                if (nextLayer >= 0 && nextLayer <= MeasurementEditorViewModel.NumOfLayers)
                {
                    MeasurementEditorViewModel.SelectedLayer = nextLayer;
                }
            }
        }

        public void SetInkCanvasSize(double width, double height)
        {
            Canvas.Width = width;
            Canvas.Height = height;
        }


        private async void InkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (MeasurementEditorViewModel.DrawStyle == DrawStyle.None)
                return;

            if (MeasurementEditorViewModel.DrawStyle != DrawStyle.Ink)
                Canvas.EditingMode = InkCanvasEditingMode.None;
            else
                Canvas.EditingMode = InkCanvasEditingMode.Ink;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Draw square
                if (MeasurementEditorViewModel.DrawStyle == DrawStyle.Rectangle)
                {
                    System.Windows.Point endP = e.GetPosition(Canvas);
                    List<System.Windows.Point> pointList = new List<System.Windows.Point>
                    {
                        new System.Windows.Point(iniP.X, iniP.Y),
                        new System.Windows.Point(iniP.X, endP.Y),
                        new System.Windows.Point(endP.X, endP.Y),
                        new System.Windows.Point(endP.X, iniP.Y),
                        new System.Windows.Point(iniP.X, iniP.Y),
                    };
                    StylusPointCollection point = new StylusPointCollection(pointList);
                    Stroke stroke = new Stroke(point)
                    {
                        DrawingAttributes = Canvas.DefaultDrawingAttributes.Clone()
                    };

                    Canvas.Strokes.Clear();
                    Canvas.Strokes.Add(stroke);
                }
                // Draw Eclipse
                else if (MeasurementEditorViewModel.DrawStyle == DrawStyle.Ellipsis)
                {
                    System.Windows.Point endP = e.GetPosition(Canvas);
                    List<Point> pointList = GenerateEclipseGeometry(iniP, endP);
                    StylusPointCollection point = new StylusPointCollection(pointList);
                    Stroke stroke = new Stroke(point)
                    {
                        DrawingAttributes = Canvas.DefaultDrawingAttributes.Clone()
                    };
                    Canvas.Strokes.Clear();
                    Canvas.Strokes.Add(stroke);
                }
            }

            var currentPosition = e.GetPosition(Canvas);
            InkCanvasToolTip.HorizontalOffset = currentPosition.X;
            InkCanvasToolTip.VerticalOffset = currentPosition.Y;

            await MeasurementEditorViewModel.MouseMovedOnImage((int)Math.Round(currentPosition.X), (int)Math.Round(currentPosition.Y));
        }

        private void InkCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!MeasurementEditorViewModel.IsOperationCallableFromCanvas || MeasurementEditorViewModel.DrawStyle == DrawStyle.None)
                return;

            var transformedStrokes = StrokeConverter.ConvertStrokeCollection(Canvas.Strokes);
            if (transformedStrokes.Length > 0)
            {
                MeasurementEditorViewModel.StrokesCollected(transformedStrokes);
            }
            Canvas.Strokes.Clear();
        }

        private List<System.Windows.Point> GenerateEclipseGeometry(Point st, Point ed)
        {
            double a = 0.5 * (ed.X - st.X);
            double b = 0.5 * (ed.Y - st.Y);
            List<Point> pointList = new List<Point>();
            for (double r = 0; r <= 2 * Math.PI; r = r + 0.01)
            {
                pointList.Add(new Point(0.5 * (st.X + ed.X) + a * Math.Cos(r), 0.5 * (st.Y + ed.Y) + b * Math.Sin(r)));
            }
            return pointList;
        }
    }
}
