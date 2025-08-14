using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CSnakes.Runtime;
using SecurePaste.Services;

namespace SecurePaste
{
    internal static class Program
    {
        private static IHost? _host;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Check for single instance
            using var mutex = new Mutex(true, "SecurePaste_SingleInstance", out bool isFirstInstance);
            
            if (!isFirstInstance)
            {
                MessageBox.Show("SecurePaste is already running. Check the system tray.", 
                    "SecurePaste", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Configure application
            ApplicationConfiguration.Initialize();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.EnableVisualStyles();

            // Set up exception handling
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                // Setup dependency injection
                var builder = Host.CreateApplicationBuilder();
                ConfigureServices(builder.Services);
                _host = builder.Build();

                // Start the host services and verify Presidio installation
                _host.Start();
                
                // Verify and update Presidio installation status in background
                _ = Task.Run(async () => await VerifyPresidioInstallationAsync());

                // Run the main form with DI
                var mainForm = _host.Services.GetRequiredService<MainForm>();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A fatal error occurred:\n{ex.Message}", 
                    "SecurePaste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _host?.Dispose();
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Python configuration
            var pythonHome = Environment.CurrentDirectory;
            var venv = Path.Combine(pythonHome, ".venv");

            // Register configuration service first
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            // Create a temporary service provider to get configuration
            using var tempServiceProvider = services.BuildServiceProvider();
            var configService = tempServiceProvider.GetRequiredService<IConfigurationService>();
            var config = configService.GetConfiguration();

            // Configure Python environment based on installation status
            var pythonBuilder = services
                .WithPython()
                .WithHome(pythonHome)
                .WithVirtualEnvironment(venv);

            // Only use pip installer if Presidio is not installed
            if (!config.PresidioInstalled)
            {
                pythonBuilder = pythonBuilder.WithPipInstaller();
            }

            pythonBuilder.FromRedistributable();

            // Register other services
            services.AddSingleton<IPresidioService, PresidioService>();
            
            // Register forms (transient as they shouldn't be singletons)
            services.AddTransient<MainForm>();
        }

        private static async Task VerifyPresidioInstallationAsync()
        {
            if (_host == null) return;

            try
            {
                var configService = _host.Services.GetRequiredService<IConfigurationService>();
                var config = configService.GetConfiguration();
                
                // Only verify if not already marked as installed
                if (!config.PresidioInstalled)
                {
                    // Give time for Python environment to be set up
                    await Task.Delay(5000);
                    
                    var presidioService = _host.Services.GetRequiredService<IPresidioService>();
                    await presidioService.VerifyAndUpdateInstallationStatusAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to verify Presidio installation: {ex}");
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"An error occurred:\n{e.Exception.Message}", 
                "SecurePaste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"A fatal error occurred:\n{ex.Message}", 
                    "SecurePaste Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}