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

                // Start the host services
                _host.Start();

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

            services
                .WithPython()
                .WithHome(pythonHome)
                .WithVirtualEnvironment(venv)
                .WithPipInstaller()
                .FromRedistributable();

            // Register services as singletons
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<IPresidioService, PresidioService>();
            
            // Register forms (transient as they shouldn't be singletons)
            services.AddTransient<MainForm>();
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