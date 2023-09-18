using Core.Image;
using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Services;
using Core.Services.Dto;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;
using Core.Operation;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Model;
using static Core.Operation.ManualEditOperation;
using GongSolutions.Wpf.DragDrop;
using System.Windows.Controls;
using System.Windows;

namespace ViewModel
{
    public class MeasurementEditorViewModel : ViewModelBase
    {
        private bool showRawImageOnly = false;
        public bool isZoomKeyPressed = false;

        private readonly IGatewayService _gatewayService;
        private readonly ICommunicationMediator _communicationMediator;
        private readonly IOperationDrawAttributesService _operationDrawAttributesService;
        private readonly IBlobId2ColorConverterService _blobAppearanceEngineService;

        #region ProgressBar Bindings
        private int progressBarValue;
        private bool progressBarIndeterminate = false;
        private bool cancelButtonVisibility = false;

        public bool ProgressBarIndeterminate
        {
            get => progressBarIndeterminate;
            set
            {
                progressBarIndeterminate = value;
                OnPropertyChanged();
            }
        }

        public int ProgressBarValue
        {
            get => progressBarValue;
            set
            {
                progressBarValue = value;
                OnPropertyChanged();
            }
        }
        public bool CancelButtonVisibility
        {
            get => cancelButtonVisibility;
            set
            {
                cancelButtonVisibility = value;
                OnPropertyChanged();
            }
        }
        #endregion


        private HistoryList _hlist;
        public HistoryList HList
        {
            get
            {
                if (_hlist is null) _hlist = new HistoryList();
                return _hlist;
            }
        }

        private OperationProperties _operationProperties;
        public OperationProperties OpProperties
        {
            get => _operationProperties;
            set
            {
                _operationProperties = value;
                OnPropertyChanged();
                OperationSettingsChanged();
            }
        }

        public List<OperationDto> Operations { get; set; }

        private OperationDto _selectedOperation;
        public OperationDto SelectedOperation
        {
            get => _selectedOperation;
            set
            {
                _selectedOperation = value;
                OnPropertyChanged();
                OpProperties = _selectedOperation.DefaultOperationProperties;
            }
        }


        private int _selectedLayer;
        public int SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                if (_selectedLayer != value)
                {
                    _selectedLayer = value;
                    _gatewayService.SetCurrentLayer(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedLayerInMmText));
                    RefreshBackground();
                    //setHistory();
                }
            }
        }

        private double layerThickness;
        public string SelectedLayerInMmText => $"{Math.Round(_selectedLayer * layerThickness, 3)} mm";

        private MaterialSampleDto _materialSample;

        public async Task CloneMeasurementOperations()
        {
            var measurementId = _communicationMediator.CloningMeasurementId;

            var operationList = await _gatewayService.ListOperationContextsAsync(measurementId);
            OperationSequence.Clear();
            foreach (var operationContext in operationList)
            {
                var sequenceItem = new OperationSequenceItem(operationContext);
                OperationSequence.Add(sequenceItem);
            }
        }

        private List<HistoryStepDto> _history;
        public List<HistoryStepDto> History
        {
            get => _history;
            set
            {
                _history = value;
                OnPropertyChanged();
            }
        }

        public int NumberOfLayers { get; set; }

        public MeasurementEditorViewModel(IGatewayService gatewayService,
                                            ICommunicationMediator communicationMediator,
                                            IOperationDrawAttributesService operationDrawAttributesService,
                                            IBlobId2ColorConverterService blobAppearanceEngineService)
        {
            ProgressBarValue = 0;
            _gatewayService = gatewayService;
            _communicationMediator = communicationMediator;
            _operationDrawAttributesService = operationDrawAttributesService;
            _blobAppearanceEngineService = blobAppearanceEngineService;

            Operations = _gatewayService.ListOperations().ToList();
            SelectedOperation = Operations[0];

            RefreshBackground();
            InitHistory();
            setHistory();

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += levelTick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);

            rangeTimer = new System.Windows.Threading.DispatcherTimer();
            rangeTimer.Tick += rangeTick;
            rangeTimer.Interval = new TimeSpan(0, 0, 1);
        }
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        System.Windows.Threading.DispatcherTimer rangeTimer;

        private void levelTick(object sender, EventArgs e)
        {
            Debug.WriteLine("LEVEL TICK");
            var task = _gatewayService.UpdateMaterialSampleAsync(_materialSample);
            task.Wait();
            _lastMinMaxModified = DateTime.UtcNow;
            _minmaxRefreshNeeded = true;
            RefreshBackground();
            dispatcherTimer.Stop();
        }

        private void rangeTick(object sender, EventArgs e)
        {
            Debug.WriteLine("Range TICK");
            var task = _gatewayService.UpdateMaterialSampleAsync(_materialSample);
            task.Wait();
            _lastMinMaxModified = DateTime.UtcNow;
            _minmaxRefreshNeeded = true;
            RefreshBackground();
            rangeTimer.Stop();
        }

        public void Window_Loaded()
        {
            var session = _gatewayService.GetCurrentSession();
            NumberOfLayers = session.CurrentMeasurement.MaterialSample.RawImages.NumberOfLayers - 1;
            OnPropertyChanged("NumberOfLayers");
            SelectedLayer = session.CurrentLayer;
            _materialSample = session.CurrentMeasurement.MaterialSample;

            _dicomLevel = _materialSample.RawImages.DicomLevel;
            _dicomRange = _materialSample.RawImages.DicomRange;
            OnPropertyChanged("DicomLevelValue");
            OnPropertyChanged("DicomRangeValue");

            if (_materialSample.MaterialScan.ScanFileFormat == Core.Enums.ScanFileFormat.DICOM)
            {
                DicomSettingsVisible = true;
            }
        }

        #region Operation sequence
        public OperationSequenceCollection OperationSequence { get; set; } = new OperationSequenceCollection();

        public class OperationSequenceCollection : ObservableCollection<OperationSequenceItem>
        {
            private bool _allowOperationSequenceReordering;
            public bool AllowOperationSequenceReordering
            {
                get
                {
                    return _allowOperationSequenceReordering;
                }
                set
                {
                    _allowOperationSequenceReordering = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(AllowOperationSequenceReordering)));
                }
            }

            public OperationSequenceCollection() : base()
            {
                CollectionChanged += OperationSequence_CollectionChanged;
                AllowOperationSequenceReordering = true;
            }

            private void OperationSequence_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.OldItems != null)
                {
                    foreach (INotifyPropertyChanged item in e.OldItems)
                        item.PropertyChanged -= item_PropertyChanged;
                }
                if (e.NewItems != null)
                {
                    foreach (INotifyPropertyChanged item in e.NewItems)
                        item.PropertyChanged += item_PropertyChanged;
                }
                CheckReorderingCondition();
            }

            private void item_PropertyChanged(object sender, PropertyChangedEventArgs e) => CheckReorderingCondition();

            private void CheckReorderingCondition()
            {
                AllowOperationSequenceReordering = !(this.Any(item => item.State != OperationSequenceItem.States.Default));
            }
        }

        public class OperationSequenceItem : INotifyPropertyChanged
        {

            public enum States
            {
                Default,
                Running,
                Queued
            }

            private States _state;
            public States State
            {
                get => _state;
                set
                {
                    _state = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }

            public OperationContext OperationContext { get; set; }

            public OperationSequenceItem(OperationContext context)
            {
                OperationContext = context;
                State = States.Default;
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public void OperationSelectedFromSequence(int index)
        {
            OpProperties = OperationSequence[index].OperationContext.OperationProperties;
            var operationName = OperationSequence[index].OperationContext.OperationName;
            SelectedOperation = Operations.FirstOrDefault(o => o.Name == operationName);
        }

        public void RemoveOperationFromSequence(int index)
        {
            OperationSequence.RemoveAt(index);
        }

        public void AddSelectedOperationToSequence()
        {
            var sequenceItem = new OperationSequenceItem(new OperationContext() { OperationName = SelectedOperation.Name, OperationProperties = OpProperties });
            OperationSequence.Add(sequenceItem);
        }

        public async Task RunSequence(int index)
        {
            IsOperationRunning = true;
            CancelButtonVisibility = true;

            var operationsToRun = new List<OperationSequenceItem>();
            for (int i = 0; i <= index; i++)
            {
                var sequenceItem = OperationSequence[i];
                if (sequenceItem.State == OperationSequenceItem.States.Default)
                {
                    sequenceItem.State = OperationSequenceItem.States.Queued;
                    operationsToRun.Add(sequenceItem);
                }
            }

            StartPollingOperationProgress();

            foreach (var sequenceItem in operationsToRun)
            {
                if (_cancelRequested)
                {
                    sequenceItem.State = OperationSequenceItem.States.Default;
                }
                else
                {
                    sequenceItem.State = OperationSequenceItem.States.Running;
                    await RunOperation(
                            new OperationRunEventArgs(),
                            sequenceItem.OperationContext.OperationProperties,
                            sequenceItem.OperationContext.OperationName
                            );
                    OperationSequence.Remove(sequenceItem);
                }
            }

            IsOperationRunning = false;
            CancelButtonVisibility = false;
            _cancelRequested = false;
        }

        public async Task RunSelectedOperation(OperationRunEventArgs operationRunEventArgs)
        {
            if (IsOperationRunning) return;
            IsOperationRunning = true;
            CancelButtonVisibility = true;
            StartPollingOperationProgress();
            await RunOperation(operationRunEventArgs, OpProperties, SelectedOperation.Name);
            CancelButtonVisibility = false;
            IsOperationRunning = false;
            _cancelRequested = false;
        }

        private bool isOperationRunning;
        public bool IsOperationRunning
        {
            get => isOperationRunning;
            set
            {
                if (isOperationRunning != value)
                {
                    isOperationRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged("IsOperationNotRunning");
                }
            }
        }
        public bool IsOperationNotRunning
        {
            get => !isOperationRunning;
        }
        #endregion

        #region Inkcanvas Settings Properties and bindings
        public bool DrawRectangle => SelectedOperationDrawStyle == DrawStyle.Rectangle;

        public bool DrawEllipsis => SelectedOperationDrawStyle == DrawStyle.Ellipsis;

        private List<DrawStyle> _operationDrawStyles;
        public List<DrawStyle> OperationDrawStyles
        {
            get
            {
                if (_operationDrawStyles == null)
                {
                    _operationDrawStyles = new List<DrawStyle>();
                    foreach (var item in Enum.GetValues(typeof(DrawStyle)).Cast<DrawStyle>())
                    {
                        _operationDrawStyles.Add(item);
                    }

                }
                return _operationDrawStyles;
            }
            set
            {
                _operationDrawStyles = value;
            }
        }
        public DrawStyle SelectedOperationDrawStyle { get; set; } = DrawStyle.Ink;
        public bool ShowRawImageOnly
        {
            get => showRawImageOnly;
            set
            {
                showRawImageOnly = value;
                RefreshBackground();
            }
        }

        TimeSpan _minTimeBetweenPixelInformationUpdates = TimeSpan.FromMilliseconds(50);
        private DateTime _lastPixelInformationUpdateTime = DateTime.UtcNow;

        private PixelInformationDto _pixelInformation;
        public PixelInformationDto PixelInformation
        {
            get => _pixelInformation;
            set
            {
                _pixelInformation = value;
                OnPropertyChanged();
            }
        }

        private bool _showRawPixelValues;
        public bool ShowRawPixelValues
        {
            get => _showRawPixelValues;
            set
            {
                _showRawPixelValues = value;
                OnPropertyChanged();
                UpdatePixelInfoVisibilities();
                ShowBitmapPixelValues = value;
                ShowDicomPixelValues = value && DicomSettingsVisible;
            }
        }

        private bool _showBitmapPixelValues;
        public bool ShowBitmapPixelValues
        {
            get => _showBitmapPixelValues;
            set
            {
                _showBitmapPixelValues = value;
                OnPropertyChanged();
            }
        }

        private bool _showDicomPixelValues;
        public bool ShowDicomPixelValues
        {
            get => _showDicomPixelValues;
            set
            {
                _showDicomPixelValues = value;
                OnPropertyChanged();
            }
        }

        private bool _showBlobPixelInfo;
        public bool ShowBlobPixelInfo
        {
            get => _showBlobPixelInfo;
            set
            {
                _showBlobPixelInfo = value;
                OnPropertyChanged();
                UpdatePixelInfoVisibilities();
            }
        }

        private bool _showCoordinates;
        public bool ShowCoordinates
        {
            get => _showCoordinates;
            set
            {
                _showCoordinates = value;
                OnPropertyChanged();
                UpdatePixelInfoVisibilities();
            }
        }

        private bool _showAnyPixelInfo;
        public bool ShowAnyPixelInfo
        {
            get => _showAnyPixelInfo;
            set
            {
                _showAnyPixelInfo = value;
                OnPropertyChanged();
            }
        }

        private bool _showPixelInfoSeparator1;
        public bool ShowPixelInfoSeparator1
        {
            get => _showPixelInfoSeparator1;
            set
            {
                _showPixelInfoSeparator1 = value;
                OnPropertyChanged();
            }
        }

        private bool _showPixelInfoSeparator2;
        public bool ShowPixelInfoSeparator2
        {
            get => _showPixelInfoSeparator2;
            set
            {
                _showPixelInfoSeparator2 = value;
                OnPropertyChanged();
            }
        }

        private void UpdatePixelInfoVisibilities()
        {
            ShowAnyPixelInfo = ShowCoordinates || ShowBlobPixelInfo || ShowRawPixelValues;
            ShowPixelInfoSeparator1 = ShowCoordinates && ShowBlobPixelInfo;
            ShowPixelInfoSeparator2 = (ShowCoordinates || ShowBlobPixelInfo) && ShowRawPixelValues;
        }

        private System.Drawing.Point _currentCoordinates;
        public System.Drawing.Point CurrentCoordinates
        {
            get => _currentCoordinates;
            set
            {
                _currentCoordinates = value;
                OnPropertyChanged();
            }
        }

        public async Task MouseMovedOnImage(int x, int y)
        {
            if (!ShowAnyPixelInfo) return;
            var currentTime = DateTime.UtcNow;
            if (currentTime - _lastPixelInformationUpdateTime >= _minTimeBetweenPixelInformationUpdates)
            {
                var pixelInformation = await _gatewayService.GetPixelInformationAsync(x, y);
                PixelInformation = pixelInformation;
                _lastPixelInformationUpdateTime = currentTime;
                CurrentCoordinates = new System.Drawing.Point(x, y);
            }
        }
        #endregion

        public IDropTarget DropTargetForTabControl { get; set; } = new TabControlDropTarget();

        public class TabControlDropTarget : IDropTarget
        {
            void IDropTarget.DragOver(IDropInfo dropInfo)
            {
                TabItem sourceItem = dropInfo.Data as TabItem;
                ItemCollection targetCollection = dropInfo.TargetCollection as ItemCollection;

                if (sourceItem != null && targetCollection != null)
                {
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                }
                dropInfo.Effects = DragDropEffects.Move;
            }

            void IDropTarget.Drop(IDropInfo dropInfo)
            {
                TabItem sourceItem = dropInfo.Data as TabItem;
                ItemCollection targetCollection = dropInfo.TargetCollection as ItemCollection;
                ItemCollection sourceColection = dropInfo.DragInfo.SourceCollection as ItemCollection;

                if (targetCollection == null)
                {
                    TabControl tabControl = dropInfo.VisualTarget as TabControl;
                    targetCollection = tabControl.Items;
                }

                sourceColection.Remove(sourceItem);
                if (sourceColection == targetCollection)
                {
                    int index = dropInfo.InsertIndex;
                    if (targetCollection.Count > index)
                    {
                        targetCollection.Insert(index, sourceItem);
                    }
                    else
                    {
                        targetCollection.Add(sourceItem);
                    }
                }
                else
                {
                    targetCollection.Insert(dropInfo.InsertIndex, sourceItem);
                }

            }
        }

        #region Operation
        private async Task RunOperation(OperationRunEventArgs operationRunEventArgs, OperationProperties properties, string operationName)
        {
            // Check if _gatewayService is ready - currently it only handles one operation at a time
            if (_gatewayService.RunningOperationProgress != 1) return;

            await Task.Run(async () =>
            {
                await _gatewayService.RunOperationAsync(
                    operationName,
                    properties,
                    operationRunEventArgs
                    );
            });

            if (!_cancelRequested)
            {
                setHistory();
                RefreshBackground();
                _communicationMediator.PlotCountChanged();
            }
        }

        private void StartPollingOperationProgress()
        {
            BackgroundWorker _backgroundWorker = new();
            _backgroundWorker.DoWork += BackgroundWorker_PollOperationProgress;
            _backgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorker_PollOperationProgress(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(300);
            double progress = _gatewayService.RunningOperationProgress;
            while (IsOperationRunning)
            {
                if (progress > 0)
                    ProgressBarIndeterminate = false;
                else
                    ProgressBarIndeterminate = true;
                progress = _gatewayService.RunningOperationProgress;
                ProgressBarValue = (int)(progress * 100);
                Thread.Sleep(100);
            }
            ProgressBarValue = 0;
            ProgressBarIndeterminate = false;
        }

        private bool _cancelRequested;

        public void CancelCurrentOperation()
        {
            _gatewayService.CancelCurrentRunningOperation();
            _cancelRequested = true;
            CancelButtonVisibility = false;
        }

        public void UndoStep() => CallHistory(true, false);

        public void RedoStep() => CallHistory(false, true);

        public async void CallHistory(bool undo, bool redo)
        {
            if (!IsOperationRunning)
            {
                IsOperationRunning = true;
                ProgressBarIndeterminate = true;
                OnPropertyChanged("IsOperationCallableFromButton");

                await Task.Run(async () =>
                {
                    if (undo)
                        await _gatewayService.UndoOperation();
                    if (redo)
                        await _gatewayService.RedoOperation();
                });

                ProgressBarIndeterminate = false;
                IsOperationRunning = false;
                setHistory();
                RefreshBackground();

                OnPropertyChanged("IsOperationCallableFromButton");
            }
        }

        public Action<double, double> RefreshBackgroundAction;
        #endregion

        public void setHistory()
        {
            //temp
            var hist = _gatewayService.ListHistory();
            var hh = new List<HistoryStepDto>(hist.History);

            //var list = hh.GetRange(0, hist.CurrentStep);
            //List<bool> isActive = new List<bool>(list.Count);
            //isActive.Range(0, hist.CurrentStep) = true;

            var isActive = Enumerable.Range(0, hh.Count).Select((n, i) =>
            {
                if (i < hist.CurrentStep)
                    return true;
                else
                    return false;
            }).ToList();

            hh.Reverse();
            isActive.Reverse();


            HList.History = hh; // list;
            HList.ItemActivity = isActive;
            HList.ActiveItem = hist.CurrentStep;
        }
        private void InitHistory()
        {
            _gatewayService.ClearHistory();
        }


        public async Task SaveSessionAsync()
        {
            await _gatewayService.SaveSessionAsync();
        }


        #region View and drawing

        #region Operation call rules 
        public async void OperationSettingsChanged()
        {
            (IsOperationCallableFromCanvas, IsOperationCallableFromButton) = await _gatewayService.GetOperationDrawSettings(SelectedOperation.Name, OpProperties);
        }

        private bool isOperationCallableFromCanvas = true;
        private bool isOperationCallableFromButton = true;
        public bool IsOperationCallableFromCanvas
        {
            get => isOperationCallableFromCanvas;
            set
            {
                if (isOperationCallableFromCanvas != value)
                {
                    isOperationCallableFromCanvas = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsOperationCallableFromButton
        {
            get => isOperationCallableFromButton && IsOperationNotRunning;
            set
            {
                if (isOperationCallableFromButton != value)
                {
                    isOperationCallableFromButton = value;
                    OnPropertyChanged();
                    OnPropertyChanged("RunOperationButtonContent");
                }
            }
        }

        public string RunOperationButtonContent
        {
            get => IsOperationCallableFromButton ? "Run Operation" : "Please draw to canvas";
        }
        #endregion


        #region DicomSettings
        TimeSpan _notModifyTime = TimeSpan.FromMilliseconds(500);
        private int _dicomRange;
        private int _dicomLevel;
        private DateTime _lastMinMaxModified;
        private bool _minmaxRefreshNeeded;
        private bool _dicomSettingsVisible;

        public bool DicomSettingsVisible
        {
            get => _dicomSettingsVisible;
            set
            {
                _dicomSettingsVisible = value;
                OnPropertyChanged();
            }
        }
        public int DicomLevelValue
        {
            get => _dicomLevel;
            set
            {
                _dicomLevel = value;
                _materialSample.RawImages.DicomLevel = value;
                OnPropertyChanged();
                if (!_minmaxRefreshNeeded)
                {
                    dispatcherTimer.Stop();
                    dispatcherTimer.Start();
                }
            }
        }

        public int DicomRangeValue
        {
            get => _dicomRange;
            set
            {
                _dicomRange = value;
                _materialSample.RawImages.DicomRange = value;
                OnPropertyChanged();
                if (!_minmaxRefreshNeeded)
                {
                    rangeTimer.Stop();
                    rangeTimer.Start();
                }
            }
        }
        #endregion

        private ImageSource _background;
        public ImageSource Background
        {
            get => _background;
            set
            {
                _background = value;
                OnPropertyChanged();
            }
        }

        private string _currentLayerFilePath;
        public string CurrentLayerFilePath
        {
            get => _currentLayerFilePath;
            set
            {
                _currentLayerFilePath = value;
                OnPropertyChanged();
            }
        }

        private int _currentAppearance = 1;

        public int CurrentAppearance
        {
            get => _currentAppearance;
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

        public async void RefreshBackground()
        {
            if (_minmaxRefreshNeeded && DateTime.UtcNow - _lastMinMaxModified < _notModifyTime)
                await Task.Delay(_notModifyTime);
            _minmaxRefreshNeeded = false;

            layerThickness = _gatewayService.GetLayerThicknessInMm();
            var group = new DrawingGroup();
            switch (CurrentAppearance)
            {
                case 0:
                    await AddRawLayer(group);
                    break;
                case 1:
                    await AddLayers(0, group);
                    break;
                case 2:
                    var layer = await GetLayer(2);
                    if (layer == null)
                        await AddRawLayer(group);
                    if (layer != null)
                        AddLayer(group, layer);
                    break;
                case 3:
                    await AddLayers(1, group);
                    break;
                case 4:
                    await AddLayers(2, group);
                    break;
                case 5:
                    await AddLayers(3, group);
                    break;
                default:
                    break;
            }
            Background = new DrawingImage(group);
            if (RefreshBackgroundAction != null)
                RefreshBackgroundAction(Background.Width, Background.Height);
        }

        private async Task<Bitmap> GetLayer(int layerId)
        {
            (Bitmap, string) res = await _gatewayService.GetLayer(layerId);
            CurrentLayerFilePath = res.Item2;
            return res.Item1;
        }

        private async Task AddRawLayer(DrawingGroup group)
        {
            var rawLayer = await GetLayer(0);
            AddLayer(group, rawLayer);
        }
        private void AddLayer(DrawingGroup group, Bitmap layer)
        {
            var drawing = new ImageDrawing(BitmapToImageSource(layer), new System.Windows.Rect(0, 0, layer.Width, layer.Height));
            group.Children.Add(drawing);
        }

        private async Task AddLayers(int appearanceId, DrawingGroup group)
        {
            _blobAppearanceEngineService.SelectAppearance(appearanceId);
            var rawLayer = await GetLayer(0);
            var blobLayer = await GetLayer(1);
            if (rawLayer != null && blobLayer != null)
            {
                if (appearanceId == 0)
                    AddLayer(group, rawLayer);
                AddLayer(group, blobLayer);
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public class BackgroundImage : INotifyPropertyChanged
        {
            private BitmapImage _bitmapImage;

            public event PropertyChangedEventHandler PropertyChanged;

            public BackgroundImage()
            {

            }

            public BackgroundImage(BitmapImage bitmapImage)
            {
                _bitmapImage = bitmapImage;
            }

            public BitmapImage BitmapImage
            {
                get
                {
                    return _bitmapImage;
                }
                set
                {
                    _bitmapImage = value;
                    OnPropertyChanged();
                }
            }

            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion
    }

    #region History
    public class HistoryList : INotifyPropertyChanged
    {
        private IEnumerable<HistoryStepDto> _history;
        private IEnumerable<bool> _itemActivity;
        private int _activeItem;

        public event PropertyChangedEventHandler PropertyChanged;

        public HistoryList()
        {
        }

        public HistoryList(IEnumerable<HistoryStepDto> history, IEnumerable<bool> activity)
        {
            _history = history;
            _itemActivity = activity;
        }

        public IEnumerable<HistoryStepDto> History
        {
            get
            {
                return _history;
            }
            set
            {
                _history = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<bool> ItemActivity
        {
            get
            {
                return _itemActivity;
            }
            set
            {
                _itemActivity = value;
                OnPropertyChanged();
            }
        }

        public int ActiveItem
        {
            get
            {
                return _activeItem;
            }
            set
            {
                _activeItem = value;
                OnPropertyChanged();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
    #endregion
}
