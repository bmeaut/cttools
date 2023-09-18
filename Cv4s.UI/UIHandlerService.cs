using Cv4s.Common.Enums;
using Cv4s.Common.Exceptions;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.UI.View;
using Cv4s.UI.ViewModel;
using Microsoft.Win32;
using OxyPlot;
using System;
using System.Threading.Tasks;

namespace Cv4s.UI
{
    public delegate Task OnClickEventHandler();

    public class UIHandlerService : IUIHandlerService
    {
        private MeasurementEditor _measurementEditor;
        private ReadFileDialog _readFileDialog;
        private OxyPlotViewer _oxyPlotViewer;

        //private ReadFiles _readFiles;
        private readonly IBlobAppearanceService _blobAppearanceService;

        public UIHandlerService(IBlobAppearanceService blobAppearanceService)
        {
            _blobAppearanceService = blobAppearanceService;
        }

        public void ShowMeasurementEditor(OperationBase operation, IRawImageSource RawImages, IBlobImageSource BlobImages)
        {
            MeasurementEditorViewModel viewModel = new MeasurementEditorViewModel(_blobAppearanceService, operation, RawImages, BlobImages);
            _measurementEditor = new MeasurementEditor(viewModel);

            var result = _measurementEditor.ShowDialog();

            if (result != true)
                throw new MeasurementCancelledException();
        }

        public (ScanFileFormat Format, string[] Files)? ShowFileReaderDialog()
        {
            _readFileDialog = new ReadFileDialog();

            _readFileDialog.ShowDialog();

            if (_readFileDialog.DialogResult == false)
                return null;

            return (_readFileDialog.SelectedFormat, _readFileDialog.SelectedFiles);
        }

        public void ShowOxyPlotViewer(PlotModel model)
        {
            _oxyPlotViewer = new OxyPlotViewer(new OxyPlotViewerViewModel(model));

            _oxyPlotViewer.ShowDialog();
            //TODO should have close logic ? 
        }
    }
}
