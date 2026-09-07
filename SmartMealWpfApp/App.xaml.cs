using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.Builder;
using Serilog;
using SmartMealWpfApp.Configuration;
using SmartMealWpfApp.Storage;
using SmartMealWpfApp.ViewModels;
using SmartMealWpfApp.Views;

namespace SmartMealWpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static IConfiguration _configuration = null!;
        private static ServiceProvider _serviceProvider = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            RxAppBuilder.CreateReactiveUIBuilder()
                .WithWpf()
                .WithViewsFromAssembly(typeof(App).Assembly) // auto-register all IViewFor in this assembly
                .WithCacheSizes(smallCacheLimit: 100, bigCacheLimit: 400) // Customize cache sizes
                .WithExceptionHandler(Observer.Create<Exception>(static ex =>
                {
                    Trace.WriteLine($"[ReactiveUI] Unhandled exception: {ex}");
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }
                }))
                .WithMessageBus()
                .Build();

            InitializeConfiguration();
            InitializeLogger();
            InitializeServices();

            base.OnStartup(e);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        private static void InitializeLogger()
        {
            var logFileName = $"test-sms-wpf-app-{DateTime.Now:yyyyMMdd}.log";
            var logFilePath = Path.Combine(AppContext.BaseDirectory, logFileName);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .WriteTo.File(logFilePath,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Logging initialized. File: {LogFileName}", logFileName);
        }

        private static void InitializeServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_configuration);
            services.Configure<EnvironmentVariablesOptions>(_configuration.GetSection(EnvironmentVariablesOptions.SectionName));
            services.AddLogging(builder => builder.AddSerilog(dispose: false));
            services.AddSingleton<IEnvironmentVariablesStore, UserEnvironmentVariablesStore>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();
        }

    }

}
