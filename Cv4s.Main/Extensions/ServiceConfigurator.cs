using Cv4s.Common.Enums;
using Cv4s.Common.Exceptions;
using Cv4s.Common.Interfaces;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Common.Models.Images;
using Cv4s.Common.Services;
using Cv4s.Measurements;
using Cv4s.Operations.ConnectedComponents3DOperation;
using Cv4s.Operations.MultiChannelThresoldOperation;
using Cv4s.Operations.PoreSizeStatOperation;
using Cv4s.Operations.ReadImageOperation;
using Cv4s.Operations.RoiOperation;
using Cv4s.Operations.SelectMaterialsOperation;
using Cv4s.UI;
using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp;
using System.Reflection;

namespace Cv4s.Extensions
{
    public static class ServiceConfigurator
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<ITestService, TestService>();
            services.AddScoped<IImageSaveService, ImageSaveService>();
            services.AddSingleton<IUIHandlerService, UIHandlerService>();
            services.AddSingleton<IBlobAppearanceService, BlobAppearanceService>();
        }

        public static void RegisterOperations(this IServiceCollection services)
        {
            //TODO register operations
            services.AddScoped<MedianBlurOperation>();
            services.AddScoped<ReadImageOperation>();
            services.AddScoped<MultiChannelThresholdOperation>();
            services.AddScoped<RoiOperation>();
            services.AddScoped<SelectMaterialsOperation>();
            services.AddScoped<ConnectedComponents3DOperation>();
            services.AddScoped<PoreSizeStatOperation>();
        }

        public static void RegisterMeasurements(this IServiceCollection services)
        {
            services.AddScoped<TestMeasurement>();
            services.AddScoped<TestBlobImageMeasurement>();
        }

        public static void RegColorsForBlobAppearanceService(this IServiceProvider serviceProvider)
        {
            var appearanceService = serviceProvider.GetService<IBlobAppearanceService>();
            appearanceService!.SelectAppearance(0); //Initializing

            var strategyForComponent = new TagComponentRandomColorProvider(
                new GetNextRandomColorDelegate(BlobAppearanceService.GetNextRandomColor));
            var strategyForTag = new TagRandomColorProviderSameForEachComponent(
               new GetNextRandomColorDelegate(BlobAppearanceService.GetNextRandomColor));
            var strategyForHeatmap = new HeatMapColorForTag();

            var blue = new Vec4b(255, 0, 0, 255);
            var red = new Vec4b(0, 0, 255, 255);
            var green = new Vec4b(120, 120, 0, 255);
            var darkgrey = new Vec4b(0, 0, 0, 200);

            var multiChannelApp = new TagAppearanceCommand(Tags.ComponentId.ToString(), strategyForComponent, 1000);
            var poreApp = new TagAppearanceCommand(MaterialTag.PORE.ToString(), strategyForComponent, 3000);

            appearanceService.DefaultBlobColor = red;
            appearanceService.AddTagAppearanceCommand("selected", new Vec4b(255, 128, 128, 255), 1000);
            appearanceService.AddTagAppearanceCommand(MaterialTag.PORE.ToString(), blue, 2000);
            appearanceService.AddTagAppearanceCommand(MaterialTag.CEMENT.ToString(), darkgrey, 2000);
            appearanceService.AddTagAppearanceCommand(MaterialTag.STEEL_FIBER.ToString(), green, 2000);

            appearanceService.AddTagAppearanceCommand("TresholdingOperation", new Vec4b(0, 200, 200, 255), 1000);
            appearanceService.AddTagAppearanceCommand(multiChannelApp);
            appearanceService.AddTagAppearanceCommand(nameof(Tags.RemoveFromRoiTag), new Vec4b(255, 0, 120, 180), 10000);

            appearanceService.SelectAppearance(1);
            appearanceService.AddTagAppearanceCommand(poreApp);
            appearanceService.AssignRandomDefaultColors = false;

            appearanceService.SelectAppearance(2);
            appearanceService.AssignRandomDefaultColors = false;
            appearanceService.AddTagAppearanceCommand(MaterialTag.CEMENT.ToString(), darkgrey, 2000);

            appearanceService.SelectAppearance(3);
            appearanceService.AssignRandomDefaultColors = false;
            appearanceService.AddTagAppearanceCommand(MaterialTag.STEEL_FIBER.ToString(), red, 2010);

        }
    }
}