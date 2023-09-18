using Core.Operation;
using Core.Services;
using Core.Services.Dto;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ViewModel
{
    public class PageCache : ViewModelBase
    {
        public Dictionary<int, Page> Pages { get; set; }

        public List<InternalOutput> InternalOutputs;
        Dictionary<string, InternalOutput> InternalOutputsDict;


        public Action RefreshContentAction;
        private readonly IGatewayService _gatewayService;
        private readonly ICommunicationMediator _communicationMediator;
        private readonly MeasurementDto _measurement;

        public PageCache(IGatewayService gatewayService,
            ICommunicationMediator communicationMediator)
        {
            _gatewayService = gatewayService;
            _communicationMediator = communicationMediator;
            _measurement = _gatewayService.GetCurrentSession().CurrentMeasurement;

            _communicationMediator.PlotCountChanged += LoadInternalOutputs;

            Pages = new Dictionary<int, Page>();
            InternalOutputs = new List<InternalOutput>();
        }

        public void LoadInternalOutputs()
        {
            var task = _gatewayService.ListInternalOutputsAsync(_measurement.Id);
            task.Wait();
            InternalOutputsDict = task.Result;

            InternalOutputs = InternalOutputsDict.Values.ToList();
            if (RefreshContentAction != null)
                RefreshContentAction();
        }

    }
}
