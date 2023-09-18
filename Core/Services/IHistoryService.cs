using Core.Services.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public interface IHistoryService
    {
        void AddStep(IHistoryStep step);
        void StepBackward();
        void StepForward();
        HistoryDto GetHistory();
        void ClearHistory();
    }

    public interface IHistoryStep
    {
        string DisplayName { get; }
        bool IsActive { get; set; }
        bool IsCurrent { get; set; }
        void StepBackwards();
        void StepForwards();
    }
}
