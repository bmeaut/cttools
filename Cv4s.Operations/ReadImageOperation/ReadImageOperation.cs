using Cv4s.Common.Exceptions;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Common.Services;

namespace Cv4s.Operations.ReadImageOperation
{
    public class ReadImageOperation : OperationBase
    {
        private readonly IUIHandlerService _uiHandlerService;

        public ReadImageOperation(IUIHandlerService handlerService)
        {
            _uiHandlerService = handlerService;
        }


        public IRawImageSource ReadFiles() 
        {
            Console.WriteLine("Reading Images from User ... ");

            var inputResult = _uiHandlerService.ShowFileReaderDialog();

            if (!inputResult.HasValue)
                throw new MeasurementCancelledException();

            IRawImageSource result;

            Console.WriteLine("Start Reading Images... ");

            switch (inputResult!.Value.Format)
            {
                case Common.Enums.ScanFileFormat.DICOM:
                    result = new DicomReaderService(inputResult.Value.Files);
                    break;
                case Common.Enums.ScanFileFormat.PNG:
                    result = new PngReaderService(inputResult.Value.Files);
                    break;
                default: throw new BusinessException("Error during process: Not supported ScanFileFormat!");
            }

            Console.WriteLine("Image reading finished...");

            return result;
        }
    }
}
