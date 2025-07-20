# Project Documentation

## Overview

GameCopier is a modern WinUI 3 application designed to simplify the process of deploying games to USB drives and external storage devices. It provides an intuitive interface for managing game libraries and copying games to multiple drives simultaneously.

## Architecture

### Project Structure
```
GameCopier/
??? GameCopier/                     # Main WinUI 3 application
?   ??? Models/                     # Data models
?   ?   ??? Game.cs                 # Game entity with properties and size calculation
?   ?   ??? Drive.cs                # Drive entity with enhanced WMI information
?   ?   ??? DeploymentJob.cs        # Deployment operation tracking
?   ??? Services/                   # Business logic layer
?   ?   ??? LibraryService.cs       # Game library management and scanning
?   ?   ??? DriveService.cs         # Drive detection and filtering
?   ?   ??? DeploymentService.cs    # Multi-threaded game copying
?   ?   ??? SettingsService.cs      # Configuration persistence
?   ?   ??? UsbDriveDetectionService.cs # Real-time USB monitoring
?   ?   ??? LoggingService.cs       # Application logging
?   ??? ViewModels/                 # MVVM view models
?   ?   ??? MainViewModel.cs        # Primary application view model
?   ??? Views/                      # XAML user interfaces
?   ?   ??? MainWindow.xaml         # Main application window
?   ?   ??? SettingsDialog.xaml.cs  # Settings configuration dialog
?   ??? App.xaml                    # Application entry point
??? README.md                       # Project documentation
??? CONTRIBUTING.md                 # Contribution guidelines
??? CHANGELOG.md                    # Version history
??? LICENSE                         # MIT license
??? .gitignore                      # Git ignore rules
```

### Design Patterns

#### MVVM (Model-View-ViewModel)
- **Models**: Pure data entities (Game, Drive, DeploymentJob)
- **Views**: XAML-based user interfaces with minimal code-behind
- **ViewModels**: Business logic and data binding intermediaries

#### Service Layer
- **LibraryService**: Handles game discovery, library configuration, and folder management
- **DriveService**: Manages drive detection, filtering, and information retrieval using WMI
- **DeploymentService**: Orchestrates multi-threaded file copying operations
- **SettingsService**: Persists user preferences using JSON configuration files

#### Observer Pattern
- **Real-time USB Detection**: Event-driven architecture for drive insertion/removal
- **Progress Tracking**: Event-based updates for deployment progress
- **UI Updates**: Property change notifications for reactive UI updates

### Key Technologies

#### Core Framework
- **.NET 9.0**: Latest .NET runtime with performance improvements
- **C# 13.0**: Modern language features including pattern matching and records
- **WinUI 3**: Native Windows UI framework with modern design system

#### Libraries and Packages
- **CommunityToolkit.Mvvm**: MVVM helpers and commands
- **System.Management**: WMI integration for enhanced drive information
- **Microsoft.WindowsAppSDK**: Windows 11 platform integrations

#### Data Persistence
- **JSON Configuration**: Lightweight settings storage
- **File System Monitoring**: Real-time directory watching
- **Application Data**: User-specific configuration in AppData folder

## Features Deep Dive

### Game Library Management

#### Multiple Directory Support
```csharp
// LibraryService supports multiple game folders
private List<string> _gameFolders;

public async Task AddGameFolderAsync(string folderPath)
{
    if (!_gameFolders.Contains(folderPath))
    {
        _gameFolders.Add(folderPath);
        SaveGameFolders(_gameFolders);
        await ScanLibraryAsync();
    }
}
```

#### Automatic Size Calculation
- Recursive directory scanning for accurate game sizes
- Parallel processing for improved performance on large libraries
- Caching to avoid repeated calculations

#### Real-time Search
- Live filtering as user types
- Case-insensitive search across game names
- Instant results without blocking UI

### Drive Detection System

#### Enhanced Drive Information
```csharp
// DriveService uses WMI for detailed drive information
private string GetDeviceDescription(string driveLetter)
{
    // Query WMI for volume labels, file systems, and device information
    // Extract brand names and model information
    // Provide fallback descriptions for unknown devices
}
```

#### Multi-layered Filtering
1. **Drive Type Filtering**: Show/hide based on Windows drive types
2. **Individual Drive Hiding**: Specific drive letter exclusions
3. **System Drive Handling**: Optional C: drive hiding
4. **Custom Rules**: User-defined filtering logic

#### Real-time Monitoring
```csharp
// UsbDriveDetectionService provides real-time USB monitoring
public event EventHandler<UsbDriveChangedEventArgs>? UsbDriveChanged;

private void CheckForDriveChanges(object? state)
{
    var currentDrives = GetCurrentUsbDrives();
    if (!currentDrives.SetEquals(_lastKnownDrives))
    {
        // Detect added and removed drives
        // Fire events for UI updates
        // Maintain most recent drive tracking
    }
}
```

### Deployment System

#### Multi-threaded Architecture
- **Concurrent Job Processing**: Multiple games deployed simultaneously
- **Parallel File Operations**: Optimized copying using multiple threads
- **Progress Aggregation**: Real-time updates across all active jobs

#### Error Handling
- **Graceful Degradation**: Continue other jobs if one fails
- **Detailed Error Reporting**: Specific error messages for troubleshooting
- **Cancellation Support**: User can stop deployments at any time

#### Space Validation
```csharp
public bool HasSufficientSpace(Drive drive, IEnumerable<Game> games)
{
    var totalGameSize = games.Sum(g => g.SizeInBytes);
    return drive.FreeSizeInBytes >= totalGameSize;
}
```

### Settings and Configuration

#### Persistent Storage
```json
// Example drivesettings.json
{
  "ShowRemovableDrives": true,
  "ShowFixedDrives": false,
  "ShowNetworkDrives": false,
  "HideSystemDrive": true,
  "HiddenDriveLetters": ["Z:", "Y:"]
}
```

#### User Interface
- **Organized Sections**: Logical grouping of related settings
- **Real-time Validation**: Immediate feedback on configuration changes
- **Quick Actions**: Bulk operations for common scenarios

## Development Guidelines

### Code Style
- Follow Microsoft C# coding conventions
- Use XML documentation for public APIs
- Implement proper error handling and logging
- Write unit tests for business logic

### Performance Considerations
- Use async/await for I/O operations
- Implement proper cancellation token support
- Cache expensive operations when appropriate
- Profile memory usage in deployment scenarios

### Accessibility
- Support keyboard navigation throughout the application
- Provide meaningful automation names for UI elements
- Follow Windows accessibility guidelines
- Test with screen readers

### Testing Strategy
- Unit tests for service layer logic
- Integration tests for file operations
- UI tests for critical user workflows
- Performance tests for large libraries

## Deployment and Distribution

### Build Process
```bash
# Debug build for development
dotnet build --configuration Debug

# Release build for distribution
dotnet build --configuration Release

# Self-contained deployment
dotnet publish --configuration Release --self-contained true
```

### Packaging
- **MSIX Packages**: Modern Windows app packaging
- **Multi-architecture**: Support for x64, x86, and ARM64
- **Automatic Updates**: Built-in update mechanisms

### System Requirements
- **Minimum**: Windows 10 1809, .NET 9.0 Runtime
- **Recommended**: Windows 11, 8GB RAM, SSD storage
- **Permissions**: File system access, WMI queries

## Future Enhancements

### Planned Features
- **Cloud Storage Integration**: Support for OneDrive, Google Drive
- **Game Platform Integration**: Steam, Epic Games, Origin library detection
- **Advanced Filtering**: Custom game categorization and tagging
- **Batch Operations**: Queue multiple deployment sessions
- **Network Deployment**: Deploy to network shares and remote systems

### Technical Improvements
- **Performance Optimization**: Even faster file operations
- **Memory Efficiency**: Reduced memory footprint for large libraries
- **Reliability Enhancements**: Better error recovery and retry logic
- **Accessibility**: Enhanced screen reader and keyboard support

## Security Considerations

### File System Access
- **Principle of Least Privilege**: Request only necessary permissions
- **Input Validation**: Sanitize all file paths and user inputs
- **Error Handling**: Avoid exposing sensitive system information

### Data Privacy
- **Local Storage**: All configuration data stored locally
- **No Telemetry**: No user data transmitted to external servers
- **Transparent Logging**: Clear logging policies for troubleshooting

---

*This documentation is maintained alongside the codebase. For the most current information, please refer to the source code and inline documentation.*