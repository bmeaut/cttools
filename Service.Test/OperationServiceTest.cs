using AutoMapper;
using Core.Exceptions;
using Core.Interfaces.Operation;
using Core.Operation;
using Core.Services;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Service.Test
{
    public class OperationServiceTest : ServiceTestBase
    {
        private readonly OperationService _operationService;
        private readonly CancellationToken token = new CancellationToken();
        private readonly IProgress<double> progress = new Progress<double>();

        public OperationServiceTest() : base()
        {
            _operationService = new OperationService(_unitOfWork);
        }

        [Fact]
        public void RegisterOperation_RegistersOperationWithName()
        {
            var operationName = "TestOperation1";
            var mockOperation = new Mock<IOperation>();
            mockOperation.SetupGet(o => o.Name).Returns(() => operationName);

            _operationService.RegisterOperation(mockOperation.Object);

            var operations = _operationService.GetAll().ToList();
            operations.Should().HaveCount(1);
            operations[0].Name.Should().Be(operationName);
        }

        [Fact]
        public void RegisterOperation_RegistersTwoOperationsWithNames()
        {
            var operationName1 = "TestOperation1";
            var operationName2 = "TestOperation2";

            var mockOperation1 = new Mock<IOperation>();
            mockOperation1.SetupGet(o => o.Name).Returns(() => operationName1);
            var mockOperation2 = new Mock<IOperation>();
            mockOperation2.SetupGet(o => o.Name).Returns(() => operationName2);

            _operationService.RegisterOperation(mockOperation1.Object);
            _operationService.RegisterOperation(mockOperation2.Object);

            var operations = _operationService.GetAll().ToList();
            operations.Should().HaveCount(2);
            operations.Should().Contain(o => o.Name == operationName1);
            operations.Should().Contain(o => o.Name == operationName2);
        }

        [Fact]
        public void RegisterOperation_ThrowsExceptionWhenRegisteredOperation()
        {
            var operationName1 = "TestOperation1";
            var operationName2 = "TestOperation1";

            var mockOperation1 = new Mock<IOperation>();
            mockOperation1.SetupGet(o => o.Name).Returns(() => operationName1);
            var mockOperation2 = new Mock<IOperation>();
            mockOperation2.SetupGet(o => o.Name).Returns(() => operationName2);

            _operationService.RegisterOperation(mockOperation1.Object);
            Action act = () => _operationService.RegisterOperation(mockOperation2.Object);
            act.Should().Throw<OperationWithNameAlreadyRegisteredException>();

            var operations = _operationService.GetAll().ToList();
            operations.Should().HaveCount(1);
        }

        public class TestOperation2 : IOperation
        {
            public string Name => "TestOperation2";

            public OperationProperties DefaultOperationProperties => throw new NotImplementedException();

            public virtual async Task<OperationContext> Run(OperationContext context, IProgress<double> progress, CancellationToken token) => context;
        }

        [Fact]
        public async void RunOperationAsync_CallsRunWithContextOnOperation()
        {
            var measurement = (await _unitOfWork.Measurements.GetAllAsync()).ToList()[0];
            var operationName1 = "TestOperation1";
            var operationName2 = "TestOperation2";

            var mockOperation1 = new Mock<IOperation>();
            mockOperation1.SetupGet(o => o.Name).Returns(() => operationName1);
            var mockOperation2 = new Mock<IOperation>();
            mockOperation2.SetupGet(o => o.Name).Returns(() => operationName2);

            _operationService.RegisterOperation(mockOperation1.Object);
            _operationService.RegisterOperation(mockOperation2.Object);

            var context = new OperationContext
            {
                MeasurementId = measurement.Id,
                OperationName = operationName2,
            };

            mockOperation2.Setup(o => o.Run(context, progress, token)).ReturnsAsync(context);

            var result = await _operationService.RunOperationAsync(context, token, progress);

            mockOperation2.Verify(o => o.Run(context, progress, token), Times.Once());
            result.Should().Be(context);
        }

        [Fact]
        public async void RunOperationAsync_ThrowsExceptionWhenOperationNotRegistered()
        {
            var operationName1 = "TestOperation1";
            var operationName2 = "TestOperation2";

            var mockOperation1 = new Mock<IOperation>();
            mockOperation1.SetupGet(o => o.Name).Returns(() => operationName1);
            _operationService.RegisterOperation(mockOperation1.Object);

            var context = new OperationContext
            {
                OperationName = operationName2
            };
            Func<Task> act = async () => await _operationService.RunOperationAsync(context, token, progress);
            await act.Should().ThrowAsync<OperationWithNameNotFoundException>();
        }
    }
}
