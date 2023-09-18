using Core.Interfaces.Operation;
using Core.Operation;
using Core.Operation.InternalOutputs;
using Core.Services;
using Core.Services.Dto;
using Model;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ViewModel
{
    public class ResultViewerViewModel : ViewModelBase
    {
        private IGatewayService _gatewayService;
        private readonly ICommunicationMediator _communicationMediator;
        private readonly PageCache _pageCache;

        public Dictionary<int, Page> Pages { get; set; }

        public List<InternalOutput> InternalOutputs => _pageCache.InternalOutputs;

        public Action<int, InternalOutputType> RefreshContentAction;


        public int PlotCount
        {
            get => plotCount;
            set
            {
                plotCount = value;
                OnPropertyChanged();
            }
        }

        public ResultViewerViewModel(IGatewayService gatewayService,
            ICommunicationMediator communicationMediator,
            PageCache pageCache)
        {
            _gatewayService = gatewayService;
            _communicationMediator = communicationMediator;
            _pageCache = pageCache;

            Pages = pageCache.Pages;
            pageCache.RefreshContentAction += InternalOutputsChanged;
            pageCache.LoadInternalOutputs();
        }

        public void OnWindowLoaded()
        {
            if (PlotCount > 0 && CurrentPlotIndex == 0)
                CurrentPlotIndex = 1;
        }

        private void InternalOutputsChanged()
        {
            PlotCount = _pageCache.InternalOutputs.Count;
            SetButtonStates();
        }

        private int _currentPlotIndex = 0;

        public int CurrentPlotIndex
        {
            get => _currentPlotIndex;
            set
            {
                if (value > PlotCount || value < 1) return;
                _currentPlotIndex = value;
                SetButtonStates();
                var output = _pageCache.InternalOutputs[_currentPlotIndex - 1];
                //Type type = output.GetType();
                PoreDistributionInternalOutput op = output as PoreDistributionInternalOutput;
                if (op != null)
                {
                    RefreshContentAction(_currentPlotIndex - 1, InternalOutputType.PORE_DISTRIBUTION);
                }
                else
                {
                    OxyplotInternalOutput o = output as OxyplotInternalOutput;
                    if (o != null)
                        RefreshContentAction(_currentPlotIndex - 1, InternalOutputType.OXYPLOT);
                    else
                    {
                        RefreshContentAction(_currentPlotIndex - 1, InternalOutputType.NOT_SUPPORTED);
                    }
                }
                OnPropertyChanged();
            }
        }
        public Page FrameContent
        {
            get => frameContent;
            set
            {
                frameContent = value;
                OnPropertyChanged();
            }
        }

        public bool NextPlotButton_IsEnabled
        {
            get => nextPlotButton_IsEnabled;
            private set
            {
                nextPlotButton_IsEnabled = value;
                OnPropertyChanged();
            }
        }
        public bool PreviousPlotButton_IsEnabled
        {
            get => previousPlotButton_IsEnabled;
            private set
            {
                previousPlotButton_IsEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool nextPlotButton_IsEnabled;
        private bool previousPlotButton_IsEnabled;
        private int plotCount;
        private Page frameContent;

        private void SetButtonStates()
        {
            NextPlotButton_IsEnabled = CurrentPlotIndex < PlotCount;
            PreviousPlotButton_IsEnabled = CurrentPlotIndex > 1;
        }
    }

    public enum InternalOutputType
    {
        OXYPLOT,
        PORE_DISTRIBUTION,
        NOT_SUPPORTED
    }

}
