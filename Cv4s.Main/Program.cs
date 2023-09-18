using Cv4s.Extensions;
using Cv4s.Measurements;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cv4s.Main
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Starting application...");

            IHost host = Host.CreateDefaultBuilder(args)
             .ConfigureServices(services =>
             {
                 services.RegisterServices();//Services used by the UI elements and Operations
                 services.RegisterOperations();//Operations used by measurements
                 services.RegisterMeasurements(); // Measurements/OperationChains
             })
             .Build();

            Console.WriteLine("Host builded starting functionality...");
            host.Services.RegColorsForBlobAppearanceService();//Register TagCommands

            var measurement = host.Services.GetService<TestBlobImageMeasurement>();
            measurement!.RunAsync().GetAwaiter().GetResult();
        }
    }
}