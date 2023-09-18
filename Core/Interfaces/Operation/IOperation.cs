using Core.Enums;
using Core.Operation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Interfaces.Operation
{
    public interface IOperation
    {
        public string Name { get; }

        public OperationType OperationType => OperationType.INTERNAL_OPERATION; // TODO

        public OperationProperties DefaultOperationProperties { get; }

        public Task<OperationContext> Run(OperationContext context, IProgress<double> progress) =>
            Run(context, new Progress<double>(), new CancellationToken());

        public Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token);

        public Task<OperationContext> Run(OperationContext context) => Run(context, new Progress<double>());

        public bool IsCallableFromCanvas(OperationProperties operationProperties) => true;
        public bool IsCallableFromButton(OperationProperties operationProperties) => true;

    }
}
