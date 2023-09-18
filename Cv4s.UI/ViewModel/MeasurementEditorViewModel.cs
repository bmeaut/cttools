using Cv4s.Common.Enums;
using Cv4s.Common.Exceptions;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Common.Models;
using Cv4s.Common.Models.Images;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cv4s.UI.ViewModel
{
    public class MeasurementEditorViewModel : ViewModelBase
    {
        private Window _measurementEditor;

        private ImageSource _background;
        private int _selectedLayer;
        private int _labelText = 1;
        private readonly IBlobAppearanceService _blobAppearanceService;
        private IRawImageSource RawImages;
        private IBlobImageSource BlobImages;
        private OperationProperties _operationProperties;
        private OperationRunEventArgs _operationRunEventArgs;
        private bool _showCoordinates;
        private bool _showBlobPixelInfo;
        private bool _showRawPixelValues;
        private bool _isOperationCallableFromCanvas;
        private bool _isOperationCallableFromButton;
        private bool _showAnyPixelInfo;
        private bool _showPixelInfoSeparator1;
        private bool _showPixelInfoSeparator2;
        private System.Drawing.Point _currentCoordinates;
        private int _currentAppearance;
        private PixelInformation _pixelInformation;
        private bool _showDicomPixelValues;
        private bool _showBitmapPixelValues;
        private DrawStyle _drawStyle;

        private TimeSpan _minTimeBetweenPixelInformationUpdates = TimeSpan.FromMilliseconds(50);

        private DateTime _lastPixelInformationUpdateTime = DateTime.UtcNow;

        public int NumOfLayers => RawImages.NumOfLayers - 1;

        public int SelectedLayer
        {
            get
            {
                return _selectedLayer;
            }
            set
            {
                _selectedLayer = value;
                OnPropertyChanged();
                RefreshBackground();
            }
        }

        #region View properties


        public ImageSource Background
        {
            get
            {
                return _background;
            }
            set
            {
                _background = value;
                OnPropertyChanged();
            }
        }

        public Action<double, double> RefreshBackgroundAction;

        public bool ShowCoordinates
        {
            get => _showCoordinates;
            set
            {
                if (_showCoordinates != value)
                {
                    _showCoordinates = value;
                    OnPropertyChanged();
                    UpdatePixelInfoVisibilities();
                }
            }
        }

        public bool ShowBlobPixelInfo
        {
            get => _showBlobPixelInfo;
            set
            {
                if (_showBlobPixelInfo != value)
                {
                    _showBlobPixelInfo = value;
                    OnPropertyChanged();
                    UpdatePixelInfoVisibilities();
                }
            }
        }

        public bool DicomSettingsVisible { get; set; } = true;

        public bool ShowRawPixelValues
        {
            get => _showRawPixelValues;
            set
            {
                if (_showRawPixelValues != value)
                {
                    _showRawPixelValues = value;
                    OnPropertyChanged();
                    UpdatePixelInfoVisibilities();
                    ShowBitmapPixelValues = value;
                    ShowDicomPixelValues = value && DicomSettingsVisible;
                }
            }
        }

        public bool ShowBitmapPixelValues
        {
            get => _showBitmapPixelValues;
            set
            {
                _showBitmapPixelValues = value;
                OnPropertyChanged();
            }
        }

        public bool ShowDicomPixelValues
        {
            get => _showDicomPixelValues;
            set
            {
                _showDicomPixelValues = value;
                OnPropertyChanged();
            }
        }

        public bool IsOperationCallableFromCanvas
        {
            get => _isOperationCallableFromCanvas;
            set
            {
                if (_isOperationCallableFromCanvas != value)
                {
                    _isOperationCallableFromCanvas = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsOperationCallableFromButton
        {
            get => _isOperationCallableFromButton;
            set
            {
                if (_isOperationCallableFromButton != value)
                {
                    _isOperationCallableFromButton = value;
                    OnPropertyChanged();
                    OnPropertyChanged("RunOperationButtonContent");
                }
            }
        }

        public bool ShowAnyPixelInfo
        {
            get => _showAnyPixelInfo;
            set
            {
                _showAnyPixelInfo = value;
                OnPropertyChanged();
            }
        }

        public bool ShowPixelInfoSeparator1
        {
            get => _showPixelInfoSeparator1;
            set
            {
                _showPixelInfoSeparator1 = value;
                OnPropertyChanged();
            }
        }

        public bool ShowPixelInfoSeparator2
        {
            get => _showPixelInfoSeparator2;
            set
            {
                _showPixelInfoSeparator2 = value;
                OnPropertyChanged();
            }
        }

        public System.Drawing.Point CurrentCoordinates
        {
            get => _currentCoordinates;
            set
            {
                _currentCoordinates = value;
                OnPropertyChanged();
            }
        }

        public PixelInformation PixelInformation
        {
            get
            {
                return _pixelInformation;
            }
            set
            {
                _pixelInformation = value;
                OnPropertyChanged();
            }
        }

        public DrawStyle DrawStyle
        {
            get
            {
                if (!IsOperationCallableFromCanvas)
                    return DrawStyle.None;
                else
                    return _drawStyle;
            }
            set
            {
                if(_drawStyle != value && IsOperationCallableFromCanvas)
                {
                    _drawStyle = value;
                    OnPropertyChanged();
                }
            }
        }

        private void UpdatePixelInfoVisibilities()
        {
            ShowAnyPixelInfo = ShowCoordinates || ShowBlobPixelInfo || ShowRawPixelValues;
            ShowPixelInfoSeparator1 = ShowCoordinates && ShowBlobPixelInfo;
            ShowPixelInfoSeparator2 = (ShowCoordinates || ShowBlobPixelInfo) && ShowRawPixelValues;
        }
        #endregion

        #region Operation attributes
        public OperationProperties OperationProperties
        {
            get => _operationProperties;
            set
            {
                _operationProperties = value;
                OnPropertyChanged();
            }
        }

        public OperationRunEventArgs OperationRunEventArgs
        {
            get => _operationRunEventArgs;
            set
            {
                _operationRunEventArgs = value;
            }
        }

        public int CurrentAppearance
        {
            get
            {
                return _currentAppearance;
            }
            set
            {
                if (_currentAppearance != value)
                {
                    _currentAppearance = value;
                    RefreshBackground();
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        public MeasurementEditorViewModel(IBlobAppearanceService blobAppearanceService, OperationBase operation, IRawImageSource RawImages, IBlobImageSource BlobImages)
        {
            _blobAppearanceService = blobAppearanceService;
            OperationProperties = operation.Properties;
            OperationRunEventArgs = operation.RunEventArgs;
            IsOperationCallableFromCanvas = operation.RunEventArgs.IsCallableFromCanvas;
            IsOperationCallableFromButton = operation.RunEventArgs.IsCallableFromButton;
            this.RawImages = RawImages;
            this.BlobImages = BlobImages;
            SelectedLayer = 0;
        }

        public void StrokesCollected(OpenCvSharp.Point[][] strokes)
        {
            OperationRunEventArgs.CollectedStrokes = strokes;
            OperationRunEventArgs.SelectedLayer = SelectedLayer;

            _measurementEditor.DialogResult = true;
            _measurementEditor.Close();
        }

        public async Task MouseMovedOnImage(int x, int y)
        {
            var imageSize = BlobImages[0].Size;

            if (!ShowAnyPixelInfo) return;

            if ( x < 0 || imageSize.Width <= x || y < 0 || imageSize.Height <= y)
                return;

            var currentTime = DateTime.UtcNow;
            if (currentTime - _lastPixelInformationUpdateTime >= _minTimeBetweenPixelInformationUpdates)
            {
                PixelInformation = GetPixelInformation(x, y);
                _lastPixelInformationUpdateTime = currentTime;

                CurrentCoordinates = new System.Drawing.Point(x, y);
            }
        }

        private PixelInformation GetPixelInformation(int x, int y)
        {
            var blobImage = BlobImages[_selectedLayer];
            var rawImage = RawImages[_selectedLayer];

            return new PixelInformation
            {
                RawImageValue = rawImage.GetPixel(x, y).R,
                DicomValue = RawImages.GetDicomPixelValue(x, y, _selectedLayer),
                BlobId = blobImage[y, x]
            };
        }

        private void RefreshBackground()
        {
            DrawingGroup group = new DrawingGroup();

            var rawImage = RawImages[SelectedLayer];

            AddToDrawingGroup(rawImage, group);

            switch (CurrentAppearance)
            {
                case 1: //thresholded
                    AddBlobImage(0, group);
                    break;
                case 2:
                    AddBlobImage(1, group);
                    break;
                default:
                    break;
            }

            Background = new DrawingImage(group);
            RefreshBackgroundAction?.Invoke(rawImage.Width, rawImage.Height);

        }

        private void AddBlobImage(int appearance, DrawingGroup group)
        {
            _blobAppearanceService.SelectAppearance(appearance);
            var blobImage = BlobImages[SelectedLayer].GenerateBGRAImage(_blobAppearanceService);
            AddToDrawingGroup(BitmapConverter.ToBitmap(blobImage), group);
        }

        public void Window_Loaded(Window measuremnetEditor)
        {
            _measurementEditor = measuremnetEditor;
            SelectedLayer = 0;
        }


        private void AddToDrawingGroup(Bitmap image, DrawingGroup group)
        {
            var bitmapImage = GenerateInMemoryBitmapImage(image);

            var drawing = new ImageDrawing(bitmapImage, new System.Windows.Rect(0, 0, image.Width, image.Height));
            group.Children.Add(drawing);
        }


        private BitmapImage GenerateInMemoryBitmapImage(Bitmap image)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                image.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

    }
}
