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
using ViewModel;
using Core.Services.Dto;
using Core.Image;
using Core.Enums;
using System.IO;

using OpenCvSharp;
using Core.Interfaces.Image;
using Model;


namespace View
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceConfigurator _serviceConfigurator;
        public IServiceProvider ServiceProvider => _serviceConfigurator.ServiceProvider;

        public App()
        {
            ServiceCollection additionalServices = new ServiceCollection();
            AddViews(additionalServices);
            AddViewModels(additionalServices);
            _serviceConfigurator = new ServiceConfigurator(additionalServices);
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = ServiceProvider.GetService<WorkspaceOverview>();
            mainWindow.Show();
        }

        private void AddViews(ServiceCollection services)
        {
            services.AddTransient<WorkspaceOverview>();
            services.AddTransient<WorkspaceDetails>();
            services.AddTransient<MaterialSampleDetails>();
            services.AddTransient<MeasurementDetails>();
            services.AddTransient<StatusDetails>();
            services.AddTransient<MeasurementEditor>();
            services.AddTransient<ResultsViewer>();
            services.AddTransient<MeasurementOperationCloningWindow>();
        }

        private void AddViewModels(ServiceCollection services)
        {
            services.AddTransient<WorkspaceOverviewViewModel>();
            services.AddTransient<WorkspaceDetailsViewModel>();
            services.AddTransient<MaterialSampleDetailsViewModel>();
            services.AddTransient<MeasurementDetailsViewModel>();
            services.AddTransient<StatusDetailsViewModel>();
            services.AddTransient<MeasurementEditorViewModel>();
            services.AddTransient<ResultViewerViewModel>();
            services.AddScoped<PageCache>();
            services.AddTransient<MeasurementOperationCloningVM>();
        }
    }
}
