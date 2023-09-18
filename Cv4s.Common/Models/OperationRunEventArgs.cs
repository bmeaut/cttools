using OpenCvSharp;

namespace Cv4s.Common.Models
{
    public class OperationRunEventArgs
    {
        //For returning stroke params from canvas
        public Point[][]? CollectedStrokes { get; set; } = null;
        public int? SelectedLayer { get; set; } = null;

        //For Settings 
        public bool IsCallableFromCanvas { get; set; } = false;
        public bool IsCallableFromButton { get; set; } = false;
    }
}
