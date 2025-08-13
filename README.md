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

## Installation

### 1. Download and Install .NET 9.0

Download from: https://dotnet.microsoft.com/download/dotnet/9.0

### 2. Install Python and Presidio

**Option A: Automatic Installation (Recommended)**
1. Run SecurePaste
2. Right-click system tray icon ? "Install Presidio"
3. Choose "Install Full Presidio + SpaCy" for best accuracy

**Option B: Manual Installation**
```bash
# Install Python packages
pip install presidio-analyzer presidio-anonymizer

# For better accuracy, also install SpaCy
pip install spacy
python -m spacy download en_core_web_sm
```

### 3. Build and Run SecurePaste

```bash
# Clone the repository
git clone <repository-url>
cd SecurePaste

# Build the application
dotnet build --configuration Release

# Run the application
dotnet run --project SecurePaste
```

## Usage

1. **Start the Application**: Run SecurePaste.exe
2. **System Tray**: The application runs in the system tray
3. **Enable/Disable**: Right-click tray icon to toggle anonymization
4. **Configure**: Access settings through the context menu
5. **Use Normally**: Press Ctrl+V anywhere - sensitive data will be automatically anonymized

## Configuration

### General Settings
- Enable/disable anonymization
- Notification preferences
- Confidence threshold for detection
- Detection language

### Entity Configuration
- Select which entity types to detect
- Choose anonymization method for each type
- Set custom replacement values

### Python Configuration
- Set Python executable path
- Test Presidio installation
- View installation instructions

## Anonymization Methods

- **Redact**: Replace with `[REDACTED]`
- **Replace**: Replace with generic placeholder or custom text
- **Mask**: Partially hide with asterisks (e.g., `john****@email.com`)
- **Hash**: Replace with cryptographic hash

## Troubleshooting

### Presidio Not Working
1. Verify Python installation: `python --version`
2. Check Presidio installation: `pip list | grep presidio`
3. Test manually: Run "Test Installation" in the app
4. Check Python path in configuration

### Hotkey Not Working
1. Check if another application is using Ctrl+V globally
2. Run as Administrator if needed
3. Restart the application

### Performance Issues
1. Adjust confidence threshold (higher = faster, less accurate)
2. Disable unused entity types
3. Use basic Presidio installation instead of full

## Development

### Project Structure
```
SecurePaste/
??? Core/                   # Core Windows API and services
??? Models/                 # Data models and configuration
??? Services/               # Business logic services
??? Forms/                  # UI forms
??? Program.cs              # Application entry point
??? SecurePaste.csproj      # Project file
```

### Key Components
- **MainForm**: System tray and hotkey handling
- **PresidioService**: Python integration
- **ClipboardService**: Windows clipboard operations
- **ConfigurationService**: Settings management

### Building from Source
```bash
# Prerequisites
dotnet --version  # Should be 9.0 or higher

# Build
dotnet restore
dotnet build

# Run tests (if available)
dotnet test

# Publish
dotnet publish -c Release -r win-x64 --self-contained
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## Security Considerations

- SecurePaste processes clipboard data locally
- No data is sent to external servers
- Python scripts are generated and run locally
- Configuration files are stored in user's AppData folder

## Known Limitations

- Only works on Windows
- Requires Python runtime
- May have slight delay during processing
- Does not work with non-text clipboard content

## Support

For issues and questions:
1. Check the troubleshooting section
2. Review the configuration settings
3. Test Presidio installation separately
4. Create an issue with detailed error information