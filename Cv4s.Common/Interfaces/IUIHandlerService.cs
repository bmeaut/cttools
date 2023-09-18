using Cv4s.Common.Enums;
using Cv4s.Common.Interfaces.Images;
using OxyPlot;

namespace Cv4s.Common.Interfaces
{
    public interface IUIHandlerService
    {

        public void ShowMeasurementEditor(OperationBase operation, IRawImageSource RawImages, IBlobImageSource BlobImages);
        public (ScanFileFormat Format, string[] Files)? ShowFileReaderDialog();
        public void ShowOxyPlotViewer(PlotModel model);
    }
}
