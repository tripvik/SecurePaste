namespace SecurePaste
{
    internal static class Program
    {
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
                // Run the main form
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A fatal error occurred:\n{ex.Message}", 
                    "SecurePaste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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