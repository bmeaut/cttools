using Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using ViewModel;

namespace View
{
    public partial class DrawableMeasurementView : UserControl
    {
        private System.Windows.Point iniP;
        public DrawableMeasurementView()
        {
            InitializeComponent();
            SetInkCanvasDrawingAttributes();
            Focusable = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.LeftCtrl)
            {
                MeasurementEditorViewModel.isZoomKeyPressed = true;
            }
            if (e.Key == Key.H)
            {
                if (MeasurementEditorViewModel.CurrentAppearance != 0)
                {
                    _lastApperance = MeasurementEditorViewModel.CurrentAppearance;
                    MeasurementEditorViewModel.CurrentAppearance = 0;
                }
            }
            if (e.Key == Key.D0) MeasurementEditorViewModel.CurrentAppearance = 0;
            if (e.Key == Key.D1) MeasurementEditorViewModel.CurrentAppearance = 1;
            if (e.Key == Key.D2) MeasurementEditorViewModel.CurrentAppearance = 2;
            if (e.Key == Key.D3) MeasurementEditorViewModel.CurrentAppearance = 3;
            if (e.Key == Key.D4) MeasurementEditorViewModel.CurrentAppearance = 4;
            if (e.Key == Key.D5) MeasurementEditorViewModel.CurrentAppearance = 5;

        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.LeftCtrl)
            {
                MeasurementEditorViewModel.isZoomKeyPressed = false;
            }
            if (e.Key == Key.H)
                MeasurementEditorViewModel.CurrentAppearance = _lastApperance;
        }

        #region Properties for external data binding
        public Brush InkCanvasBackground
        {
            get => InkCanvas.Background;
            set => InkCanvas.Background = value;
        }

        // Note: we need to update the InkCanvas size w.r.t. the image size.
        public void SetInkCanvasSize(double width, double height)
        {
            InkCanvas.Width = width;
            InkCanvas.Height = height;
        }
        #endregion

        private void SetInkCanvasDrawingAttributes()
        {
            var inkDA = new DrawingAttributes
            {
                Color = Colors.SpringGreen,
                Height = 5,
                Width = 5,
                FitToCurve = false,
                StylusTip = StylusTip.Rectangle,
                IsHighlighter = false,
                IgnorePressure = true
            };

            InkCanvas.EditingMode = InkCanvasEditingMode.None;
            InkCanvas.DefaultDrawingAttributes = inkDA;
        }

        // Needed for running the currently selected operation
        public MeasurementEditorViewModel MeasurementEditorViewModel { get; set; }

        #region Methods handling mouse wheel and key events
        private double zoomLevel = 1.0;
        private int _lastApperance = 0;

        private void ScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MeasurementEditorViewModel.isZoomKeyPressed)
            {
                zoomLevel += (double)e.Delta / 1500.0;
                InkCanvasScaleTransform.ScaleX = zoomLevel;
                InkCanvasScaleTransform.ScaleY = zoomLevel;
                Debug.WriteLine($"Zoom level: {zoomLevel}");
            }
            if (!MeasurementEditorViewModel.isZoomKeyPressed)
            {
                int currentLayer = MeasurementEditorViewModel.SelectedLayer;
                int nextLayer = currentLayer + (e.Delta < 0 ? -1 : 1);
                if (nextLayer >= 0 && nextLayer <= MeasurementEditorViewModel.NumberOfLayers)
                {
                    MeasurementEditorViewModel.SelectedLayer = nextLayer;
                }
            }
        }


        #endregion

        private void InkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                iniP = e.GetPosition(InkCanvas);
        }

        private async void InkCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("InkCanvas StrokeCollected, calling ManualEditOperation...");
            var transformedStrokes = StrokeConverter.ConvertStrokeCollection(InkCanvas.Strokes);
            if (transformedStrokes.Length > 0)
                await MeasurementEditorViewModel.RunSelectedOperation(
                        new OperationRunEventArgs() { Strokes = transformedStrokes });
            InkCanvas.Strokes.Clear();
        }

        private async void InkCanvasMeasure_MouseMove(object sender, MouseEventArgs e)
        {
            if (MeasurementEditorViewModel.DrawRectangle || MeasurementEditorViewModel.DrawEllipsis)
                InkCanvas.EditingMode = InkCanvasEditingMode.None;
            else
                InkCanvas.EditingMode = InkCanvasEditingMode.Ink;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Draw square
                if (MeasurementEditorViewModel.DrawRectangle == true)
                {
                    System.Windows.Point endP = e.GetPosition(InkCanvas);
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
                        DrawingAttributes = InkCanvas.DefaultDrawingAttributes.Clone()
                    };

                    InkCanvas.Strokes.Clear();
                    InkCanvas.Strokes.Add(stroke);
                }
                // Draw Eclipse
                else if (MeasurementEditorViewModel.DrawEllipsis == true)
                {
                    System.Windows.Point endP = e.GetPosition(InkCanvas);
                    List<Point> pointList = GenerateEclipseGeometry(iniP, endP);
                    StylusPointCollection point = new StylusPointCollection(pointList);
                    Stroke stroke = new Stroke(point)
                    {
                        DrawingAttributes = InkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    InkCanvas.Strokes.Clear();
                    InkCanvas.Strokes.Add(stroke);
                }
            }

            var currentPosition = e.GetPosition(InkCanvas);
            InkCanvasToolTip.HorizontalOffset = currentPosition.X;
            InkCanvasToolTip.VerticalOffset = currentPosition.Y;

            await MeasurementEditorViewModel.MouseMovedOnImage((int)Math.Round(currentPosition.X), (int)Math.Round(currentPosition.Y));
        }

        private void InkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            /*Debug.WriteLine("InkCanvas StrokeCollected, calling ManualEditOperation...");
            var transformedStrokes = StrokeConverter.ConvertStrokeCollection(InkCanvas.Strokes);
            await MeasurementEditorViewModel.RunSelectedOperation(
                new OperationRunEventArgs() { Strokes = transformedStrokes });
            InkCanvas.Strokes.Clear();*/
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
