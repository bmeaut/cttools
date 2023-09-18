using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Core.Services;
using AutoMapper;
using Service;
using Core.Interfaces;
using Core.Operation;
using Core.Services.Dto;
using Core.Image;
using Core.Enums;
using OpenCvSharp;
using Core.Interfaces.Image;

namespace Model
{
    public class ServiceConfigurator
    {
        private readonly IServiceProvider _serviceProvider;
        public IServiceProvider ServiceProvider => _serviceProvider;

        public ServiceConfigurator(ServiceCollection services)
        {
            //ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            RegisterModelServices(services);

            _serviceProvider = services.BuildServiceProvider();

            RegisterOperations();
            RegisterColors();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite("Data Source = Test.db");
            });

            var mapper = new Mapper(DtoMapperConfiguration.Configuration);
            services.AddSingleton<IMapper>(mapper);

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<IWorkspaceService, WorkspaceService>();
            services.AddSingleton<IMaterialSampleService, MaterialSampleService>();
            services.AddSingleton<IOperationService, OperationService>();
            services.AddSingleton<IMeasurementService, MeasurementService>();
            services.AddSingleton<ISessionService, SessionService>();
            services.AddSingleton<IGatewayService, GatewayService>();
            services.AddSingleton<IHistoryService, HistoryService>();
            services.AddSingleton<IStatusService, StatusService>();
            services.AddSingleton<IMaterialScanService, MaterialScanService>();
            services.AddSingleton<IBlobId2ColorConverterService, BlobAppearanceEngineService>();
            services.AddSingleton<IExportImportService, ExportImportService>();
        }

        private void RegisterOperations()
        {
            var operationService = _serviceProvider.GetService<IOperationService>();
            operationService.RegisterOperation(new SteelFiberOperation());
            operationService.RegisterOperation(new DummyOperation());
            operationService.RegisterOperation(new DummyBatchOperation());
            operationService.RegisterOperation(new DummyMeasurementOperation());
            operationService.RegisterOperation(new ThresholdingOperation());
            operationService.RegisterOperation(new ManualEditOperation());
            operationService.RegisterOperation(new BlobSizeStatOperation());
            operationService.RegisterOperation(new RoIOperation()
            {
                ActionToOutputInformationRow =
                    (string info) => System.Diagnostics.Debug.WriteLine(info)
            });
            operationService.RegisterOperation(new DistanceTransformOperation());
            operationService.RegisterOperation(new ComponentAreaRatioOperation());
            operationService.RegisterOperation(new ComponentCountOperation());
            operationService.RegisterOperation(new MultiChannelThresholdingOperation());
            operationService.RegisterOperation(new AggregateRatioOperation());
            operationService.RegisterOperation(new SelectMaterialOperation());
            operationService.RegisterOperation(new QueryOperation()
            {
                ActionToOutputInformationRow =
                    (string info) => System.Diagnostics.Debug.WriteLine(info)
            });
            operationService.RegisterOperation(new DistanceMeasurementOperation()
            {
                ActionToOutputInformationRow =
                    (string info) => System.Diagnostics.Debug.WriteLine(info)
            });
            operationService.RegisterOperation(new ConnectedComponents3DOperation());
            operationService.RegisterOperation(new RoundnessShapeClassifierOperation());
            operationService.RegisterOperation(new PoreSizeStatOperation());
            operationService.RegisterOperation(new GrainDistributionCurveOperation());
            operationService.RegisterOperation(new ImageDegradationOperation());
            operationService.RegisterOperation(new MedianBlurOperation());
            //operationService.RegisterOperation(new MatlabOperation());

            operationService.ParseOperations(@"ExternalOperations");

            //var add_op = operationService.CreateMatlabOperation();
            //operationService.RegisterOperation(add_op);
        }

        private void RegisterColors()
        {
            var appearanceService = _serviceProvider.GetService<IBlobId2ColorConverterService>();
            appearanceService.SelectAppearance(0);

            var strategyForComponent = new TagComponentRandomColorProvider(
                new GetNextRandomColorDelegate(BlobAppearanceEngineService.GetNextRandomColor));
            var strategyForTag = new TagRandomColorProviderSameForEachComponent(
               new GetNextRandomColorDelegate(BlobAppearanceEngineService.GetNextRandomColor));
            var strategyForHeatmap = new HeatMapColorForTag();

            var blue = new Vec4b(255, 0, 0, 255);
            var red = new Vec4b(0, 0, 255, 255);
            var green = new Vec4b(120, 120, 0, 255);
            var darkgrey = new Vec4b(0, 0, 0, 200);

            var dummyApp = new TagAppearanceCommand("dummy", strategyForHeatmap, 1010);
            var multiChannelApp = new TagAppearanceCommand(Tags.ComponentId.ToString(), strategyForComponent, 1000);
            var poreApp = new TagAppearanceCommand(MaterialTags.PORE.ToString(), strategyForComponent, 3000);
            var roundnessApp = new TagAppearanceCommand(RoundnessShapeClassifierOperation.RoundnessTagName, strategyForHeatmap, 3100);

            appearanceService.DefaultBlobColor = red;
            appearanceService.AddTagAppearanceCommand(dummyApp);
            appearanceService.AddTagAppearanceCommand("selected", new Vec4b(255, 128, 128, 255), 1000);
            appearanceService.AddTagAppearanceCommand(MaterialTags.PORE.ToString(), blue, 2000);
            appearanceService.AddTagAppearanceCommand(MaterialTags.CEMENT.ToString(), darkgrey, 2000);
            appearanceService.AddTagAppearanceCommand(MaterialTags.STEEL_FIBER.ToString(), green, 2000);
            appearanceService.AddTagAppearanceCommand(roundnessApp);

            appearanceService.AddTagAppearanceCommand("TresholdingOperation", new Vec4b(0, 200, 200, 255), 1000);
            appearanceService.AddTagAppearanceCommand(multiChannelApp);
            appearanceService.AddTagAppearanceCommand(RoIOperation.RemoveFromRoiTagName, new Vec4b(255, 0, 120, 180), 10000);

            appearanceService.SelectAppearance(1);
            //appearanceService.AddTagAppearanceCommand(MaterialTags.PORE.ToString(), blue, 2000);
            appearanceService.AddTagAppearanceCommand(poreApp);
            appearanceService.AddTagAppearanceCommand(roundnessApp);
            appearanceService.AssignRandomDefaultColors = false;

            appearanceService.SelectAppearance(2);
            appearanceService.AssignRandomDefaultColors = false;
            appearanceService.AddTagAppearanceCommand(MaterialTags.CEMENT.ToString(), darkgrey, 2000);

            appearanceService.SelectAppearance(3);
            appearanceService.AssignRandomDefaultColors = false;
            appearanceService.AddTagAppearanceCommand(MaterialTags.STEEL_FIBER.ToString(), red, 2010);

        }

        private void RegisterModelServices(ServiceCollection services)
        {
            services.AddScoped<ICommunicationMediator, SimpleCommunicationMediator>();
            services.AddSingleton<IOperationDrawAttributesService, OperationDrawAttributes>();
        }
    }
}
