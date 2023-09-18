using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Enums;
using Core.Interfaces.Operation;
using Core.Operation;
using Core.Services.Dto;

namespace Core.Services
{
    // TODO: AutoMapper
    public interface IOperationService
    {
        double RunningOperationProgress { get; set; }

        void RegisterOperation(IOperation operation);

        IEnumerable<IOperation> GetAll();

        OperationType GetOperationType(string operationName);

        Task<OperationContext> RunOperationAsync(OperationContext context, CancellationToken token, IProgress<double> progress);

        Task AddOperationContextAsync(OperationContext context);

        void ParseOperations(string externalPluginPath);
        bool IsOperationCallableFromCanvas(string operationName, OperationProperties operationProperties);
        bool IsOperationCallableFromButton(string operationName, OperationProperties operationProperties);

        //IOperation CreatePythonOperation();

        //IOperation CreateMatlabOperation();
    }
}