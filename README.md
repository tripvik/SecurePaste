# SecurePaste

A Windows Forms application that automatically anonymizes clipboard data using Microsoft Presidio when you press Ctrl+V.

## Features

- **Global Ctrl+V Interception**: Captures all Ctrl+V operations across the system
- **Real-time PII Detection**: Uses Microsoft Presidio to identify sensitive information
- **Flexible Anonymization**: Multiple anonymization methods (redact, replace, mask, hash)
- **System Tray Integration**: Runs silently in the background with easy access
- **Configurable Entities**: Choose which types of sensitive data to detect
- **Statistics Tracking**: Monitor usage and detection patterns
- **Error Handling**: Graceful fallback to original paste if anonymization fails
- **CLI Wrapper**: Safe and robust command-line execution with timeout handling

## Supported Entity Types

- Personal names
- Email addresses
- Phone numbers
- Credit card numbers
- IBAN codes
- IP addresses
- Locations
- Dates and times
- National registration numbers
- Medical license numbers
- URLs

## Requirements

- Windows 10/11
- .NET 9.0 Runtime
- Python 3.7+ with Presidio library

## Architecture

### Core Components

- **MainForm**: System tray integration and global hotkey handling
- **PresidioService**: Python integration using CLI wrapper
- **CliWrapper**: Safe command-line execution with timeout and error handling
- **ClipboardService**: Windows clipboard operations using Win32 API
- **ConfigurationService**: Settings and statistics management

### CLI Wrapper

The `CliWrapper` class provides a robust, async-first approach to executing command-line operations:

```csharp
// Execute Python script with timeout
var result = await CliWrapper.ExecutePythonAsync(
    pythonPath, 
    scriptPath, 
    arguments, 
    new CliOptions { Timeout = TimeSpan.FromMinutes(2) }
);

// Simple command execution
var output = await CliWrapper.ExecuteAndGetOutputAsync("python", "--version");

// Test if command exists
var pythonExists = await CliWrapper.TestCommandExistsAsync("python");