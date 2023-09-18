using OpenCvSharp;
using System.Linq;
using System.Windows.Ink;
using System.Windows.Input;

namespace Cv4s.UI.Converters
{
    public static class StrokeConverter
    {
        public static Point[][] ConvertStrokeCollection(StrokeCollection strokes)
        {
            // If InkCanvas's DrawingAttributes.FitToCurve is true, GetBezierStylusPoints() has to be used
            //  instead of StylusPoints.
            return strokes.Select(s => Convert(s.StylusPoints)).ToArray();
        }

        private static Point[] Convert(StylusPointCollection points)
        {
            return points
                .Select(sp => sp.ToPoint())
                .Select(p => new Point((int)p.X, (int)p.Y))
                .ToArray();
        }
    }
}
