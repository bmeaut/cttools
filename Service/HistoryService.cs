using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Core.Image;
using Core.Services;
using Core.Services.Dto;

namespace Service
{
    public class HistoryService : IHistoryService
    {
        private List<IHistoryStep> History { get; set; } = new List<IHistoryStep>();
        private int _historyStep = 0;

        private readonly IMapper _mapper;

        public HistoryService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public void AddStep(IHistoryStep step)
        {
            // TODO: At the moment the layer change deletes all redo operations in the history as with the operations.
            //       To change that the history should be stored per layer, but the UX is questionable.
            if (_historyStep == History.Count)
            {
                History.Add(step);
                _historyStep++;
            }
            else
            {
                History.RemoveRange(_historyStep, History.Count - _historyStep);
                History.Add(step);
                _historyStep++;
            }
        }

        public void StepBackward()
        {
            // if there is no operation to reverse, don't do anything
            if (_historyStep == 0) return;
            History[_historyStep - 1].StepBackwards();

            _historyStep--;
        }

        public void StepForward()
        {
            // if there is no operation to reverse, don't do anything
            if (_historyStep == History.Count) return;

            History[_historyStep].StepForwards();

            _historyStep++;
        }

        public HistoryDto GetHistory()
        {

            foreach (var h in History)
            {
                h.IsCurrent = false;
            }
            if (_historyStep > 0)
            {
                History[_historyStep - 1].IsCurrent = true;
            }
            else
            {
                // TODO: If we are at the first point in history set it as the current and active one
                //       What should be the first history point?
                // Added InitialHistoryStep for the first history point.
                //History.Add(new InitialHistoryStep());
                //History[0].IsCurrent = true;
                //History[0].IsActive = true;
                //_historyStep++;
            }

            return new HistoryDto
            {
                CurrentStep = _historyStep,
                History = _mapper.Map<IEnumerable<HistoryStepDto>>(History)
            };
        }

        public void ClearHistory()
        {
            History.Clear();
            _historyStep = 0;
        }
    }

    class GroupOperationHistoryStep : IHistoryStep
    {
        public string DisplayName { get; }

        public bool IsActive { get; set; }

        public bool IsCurrent { get; set; }

        List<BlobImageProxy> UndoProxies { get; }
        List<BlobImageProxy> RedoProxies { get; set; }

        Object _redoProxiesLock = new Object();


        public GroupOperationHistoryStep(List<BlobImageProxy> undoProxies, string displayName)
        {
            DisplayName = displayName;
            IsActive = true;
            IsCurrent = false;
            UndoProxies = undoProxies;
            RedoProxies = new List<BlobImageProxy>();
        }

        public void StepBackwards()
        {
            IsActive = false;
            RedoProxies.Clear();
            Parallel.ForEach(UndoProxies, undoProxy =>
            {
                var proxy = undoProxy.ApplyToOriginal().CreateUndoProxy();
                lock (_redoProxiesLock)
                {
                    RedoProxies.Add(proxy);
                }
            });
        }

        public void StepForwards()
        {
            IsActive = true;
            Parallel.ForEach(RedoProxies, redoProxy =>
            {
                redoProxy?.ApplyToOriginal();
            });
        }
    }


    class OperationHistoryStep : IHistoryStep
    {
        // TOOD: create group operation history step to wrap multiple blobimage changes into one history step
        BlobImageProxy UndoProxy { get; }
        BlobImageProxy RedoProxy { get; set; }

        public string DisplayName { get; }

        public bool IsActive { get; set; }

        public bool IsCurrent { get; set; }

        public OperationHistoryStep(BlobImageProxy undoProxy, BlobImageProxy redoProxy)
        {
            DisplayName = "Operation";
            IsActive = true;
            IsCurrent = false;
            UndoProxy = undoProxy;
            RedoProxy = redoProxy;
        }

        public OperationHistoryStep(BlobImageProxy undoProxy)
        {
            DisplayName = "Operation";
            IsActive = true;
            IsCurrent = false;
            UndoProxy = undoProxy;
        }

        public OperationHistoryStep(BlobImageProxy undoProxy, string displayName)
        {
            DisplayName = displayName;
            IsActive = true;
            IsCurrent = false;
            UndoProxy = undoProxy;
        }

        public void StepBackwards()
        {
            IsActive = false;
            RedoProxy = UndoProxy.ApplyToOriginal().CreateUndoProxy();
        }

        public void StepForwards()
        {
            IsActive = true;
            RedoProxy?.ApplyToOriginal();
        }
    }

    public class LayerChangeHistoryStep : IHistoryStep
    {
        int PreviousLayer { get; }
        int CurrentLayer { get; }

        public string DisplayName { get; }

        public bool IsActive { get; set; }

        public bool IsCurrent { get; set; }

        private readonly ISessionService _sessionService;

        // TODO: figure out why DI is not injecting service in ctor
        public LayerChangeHistoryStep(ISessionService sessionService, int previousLayer, int currentLayer)
        {
            _sessionService = sessionService;

            DisplayName = $"Layer changed to { currentLayer }";
            IsActive = true;
            IsCurrent = false;
            PreviousLayer = previousLayer;
            CurrentLayer = currentLayer;
        }

        public void StepBackwards()
        {
            IsActive = false;
            _sessionService.SetCurrentLayer(PreviousLayer);
        }

        public void StepForwards()
        {
            IsActive = true;
            _sessionService.SetCurrentLayer(PreviousLayer);
        }
    }

    class InitialHistoryStep : IHistoryStep
    {
        BlobImageProxy UndoProxy { get; }
        BlobImageProxy RedoProxy { get; set; }

        public string DisplayName { get; }

        public bool IsActive { get; set; }

        public bool IsCurrent { get; set; }


        public InitialHistoryStep()
        {
            DisplayName = " ";
            IsActive = true;
            IsCurrent = false;
        }

        public void StepBackwards()
        {
        }

        public void StepForwards()
        {
        }
    }
}

