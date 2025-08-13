using System.Diagnostics;
using System.Text;

namespace SecurePaste.Services
{
    /// <summary>
    /// CLI command execution result
    /// </summary>
    public class CliResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public bool Success => ExitCode == 0;
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// CLI command execution options
    /// </summary>
    public class CliOptions
    {
        public string WorkingDirectory { get; set; } = string.Empty;
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        public bool CreateNoWindow { get; set; } = true;
        public bool UseShellExecute { get; set; } = false;
        public Encoding OutputEncoding { get; set; } = Encoding.UTF8;
        public Encoding ErrorEncoding { get; set; } = Encoding.UTF8;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// A wrapper for executing command line operations safely and efficiently
    /// </summary>
    public class CliWrapper
    {
        /// <summary>
        /// Executes a command synchronously
        /// </summary>
        /// <param name="fileName">Executable file name or path</param>
        /// <param name="arguments">Command arguments</param>
        /// <param name="options">Execution options</param>
        /// <returns>CLI execution result</returns>
        public static CliResult Execute(string fileName, string arguments = "", CliOptions? options = null)
        {
            return ExecuteAsync(fileName, arguments, options).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Executes a command asynchronously
        /// </summary>
        /// <param name="fileName">Executable file name or path</param>
        /// <param name="arguments">Command arguments</param>
        /// <param name="options">Execution options</param>
        /// <returns>CLI execution result</returns>
        public static async Task<CliResult> ExecuteAsync(string fileName, string arguments = "", CliOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            options ??= new CliOptions();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = options.UseShellExecute,
                    RedirectStandardOutput = !options.UseShellExecute,
                    RedirectStandardError = !options.UseShellExecute,
                    CreateNoWindow = options.CreateNoWindow,
                    StandardOutputEncoding = options.OutputEncoding,
                    StandardErrorEncoding = options.ErrorEncoding
                };

                // Set working directory if specified
                if (!string.IsNullOrWhiteSpace(options.WorkingDirectory))
                {
                    processStartInfo.WorkingDirectory = options.WorkingDirectory;
                }

                // Add environment variables
                foreach (var envVar in options.EnvironmentVariables)
                {
                    processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                }

                using var process = new Process { StartInfo = processStartInfo };
                
                // Start the process
                if (!process.Start())
                {
                    throw new InvalidOperationException($"Failed to start process: {fileName}");
                }

                // Set up async reading tasks
                Task<string> outputTask = Task.FromResult(string.Empty);
                Task<string> errorTask = Task.FromResult(string.Empty);

                if (!options.UseShellExecute)
                {
                    outputTask = process.StandardOutput.ReadToEndAsync();
                    errorTask = process.StandardError.ReadToEndAsync();
                }

                // Wait for completion with timeout
                using var cancellationTokenSource = new CancellationTokenSource(options.Timeout);
                try
                {
                    await process.WaitForExitAsync(cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        process.Kill(true);
                    }
                    catch
                    {
                        // Ignore kill errors
                    }
                    
                    throw new TimeoutException($"Process timed out after {options.Timeout.TotalSeconds} seconds");
                }

                // Get the results
                var output = await outputTask;
                var error = await errorTask;
                var exitCode = process.ExitCode;

                stopwatch.Stop();

                return new CliResult
                {
                    ExitCode = exitCode,
                    StandardOutput = output,
                    StandardError = error,
                    ExecutionTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                return new CliResult
                {
                    ExitCode = -1,
                    StandardOutput = string.Empty,
                    StandardError = $"Exception occurred: {ex.Message}",
                    ExecutionTime = stopwatch.Elapsed
                };
            }
        }

        /// <summary>
        /// Executes a Python script asynchronously
        /// </summary>
        /// <param name="pythonPath">Path to Python executable</param>
        /// <param name="scriptPath">Path to Python script</param>
        /// <param name="scriptArguments">Arguments to pass to the script</param>
        /// <param name="options">Execution options</param>
        /// <returns>CLI execution result</returns>
        public static async Task<CliResult> ExecutePythonAsync(string pythonPath, string scriptPath, string scriptArguments = "", CliOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(pythonPath))
                throw new ArgumentException("Python path cannot be null or empty", nameof(pythonPath));
            
            if (string.IsNullOrWhiteSpace(scriptPath))
                throw new ArgumentException("Script path cannot be null or empty", nameof(scriptPath));

            var arguments = $"\"{scriptPath}\"";
            if (!string.IsNullOrWhiteSpace(scriptArguments))
            {
                arguments += $" {scriptArguments}";
            }

            return await ExecuteAsync(pythonPath, arguments, options);
        }

        /// <summary>
        /// Executes a Python script synchronously
        /// </summary>
        /// <param name="pythonPath">Path to Python executable</param>
        /// <param name="scriptPath">Path to Python script</param>
        /// <param name="scriptArguments">Arguments to pass to the script</param>
        /// <param name="options">Execution options</param>
        /// <returns>CLI execution result</returns>
        public static CliResult ExecutePython(string pythonPath, string scriptPath, string scriptArguments = "", CliOptions? options = null)
        {
            return ExecutePythonAsync(pythonPath, scriptPath, scriptArguments, options).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Executes a command and returns only the standard output if successful
        /// </summary>
        /// <param name="fileName">Executable file name or path</param>
        /// <param name="arguments">Command arguments</param>
        /// <param name="options">Execution options</param>
        /// <returns>Standard output if successful, otherwise throws exception</returns>
        public static async Task<string> ExecuteAndGetOutputAsync(string fileName, string arguments = "", CliOptions? options = null)
        {
            var result = await ExecuteAsync(fileName, arguments, options);
            
            if (!result.Success)
            {
                throw new InvalidOperationException($"Command failed with exit code {result.ExitCode}: {result.StandardError}");
            }
            
            return result.StandardOutput;
        }

        /// <summary>
        /// Tests if a command exists and is executable
        /// </summary>
        /// <param name="command">Command to test</param>
        /// <param name="testArguments">Arguments to use for testing (default: --version)</param>
        /// <returns>True if command exists and is executable</returns>
        public static async Task<bool> TestCommandExistsAsync(string command, string testArguments = "--version")
        {
            try
            {
                var result = await ExecuteAsync(command, testArguments, new CliOptions 
                { 
                    Timeout = TimeSpan.FromSeconds(10) 
                });
                
                return result.ExitCode == 0 || result.ExitCode == 1; // Some commands return 1 for --version
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Executes a command with input via stdin
        /// </summary>
        /// <param name="fileName">Executable file name or path</param>
        /// <param name="arguments">Command arguments</param>
        /// <param name="stdinInput">Input to send to stdin</param>
        /// <param name="options">Execution options</param>
        /// <returns>CLI execution result</returns>
        public static async Task<CliResult> ExecuteWithStdinAsync(string fileName, string arguments = "", string stdinInput = "", CliOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            options ??= new CliOptions();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = options.CreateNoWindow,
                    StandardOutputEncoding = options.OutputEncoding,
                    StandardErrorEncoding = options.ErrorEncoding
                };

                // Set working directory if specified
                if (!string.IsNullOrWhiteSpace(options.WorkingDirectory))
                {
                    processStartInfo.WorkingDirectory = options.WorkingDirectory;
                }

                // Add environment variables
                foreach (var envVar in options.EnvironmentVariables)
                {
                    processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                }

                using var process = new Process { StartInfo = processStartInfo };
                
                // Start the process
                if (!process.Start())
                {
                    throw new InvalidOperationException($"Failed to start process: {fileName}");
                }

                // Write to stdin if input provided
                if (!string.IsNullOrEmpty(stdinInput))
                {
                    await process.StandardInput.WriteAsync(stdinInput);
                    process.StandardInput.Close();
                }

                // Set up async reading tasks
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // Wait for completion with timeout
                using var cancellationTokenSource = new CancellationTokenSource(options.Timeout);
                try
                {
                    await process.WaitForExitAsync(cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        process.Kill(true);
                    }
                    catch
                    {
                        // Ignore kill errors
                    }
                    
                    throw new TimeoutException($"Process timed out after {options.Timeout.TotalSeconds} seconds");
                }

                // Get the results
                var output = await outputTask;
                var error = await errorTask;
                var exitCode = process.ExitCode;

                stopwatch.Stop();

                return new CliResult
                {
                    ExitCode = exitCode,
                    StandardOutput = output,
                    StandardError = error,
                    ExecutionTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                return new CliResult
                {
                    ExitCode = -1,
                    StandardOutput = string.Empty,
                    StandardError = $"Exception occurred: {ex.Message}",
                    ExecutionTime = stopwatch.Elapsed
                };
            }
        }

        /// <summary>
        /// Executes a Python script with stdin input
        /// </summary>
        /// <param name="pythonPath">Path to Python executable</param>
        /// <param name="scriptPath">Path to Python script</param>
        /// <param name="stdinInput">Input to send to stdin</param>
        /// <param name="scriptArguments">Additional arguments to pass to the script</param>
        /// <param name="options">Execution options</param>
        /// <returns>CLI execution result</returns>
        public static async Task<CliResult> ExecutePythonWithStdinAsync(string pythonPath, string scriptPath, string stdinInput, string scriptArguments = "", CliOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(pythonPath))
                throw new ArgumentException("Python path cannot be null or empty", nameof(pythonPath));
            
            if (string.IsNullOrWhiteSpace(scriptPath))
                throw new ArgumentException("Script path cannot be null or empty", nameof(scriptPath));

            var arguments = $"\"{scriptPath}\"";
            if (!string.IsNullOrWhiteSpace(scriptArguments))
            {
                arguments += $" {scriptArguments}";
            }

            return await ExecuteWithStdinAsync(pythonPath, arguments, stdinInput, options);
        }
    }
}