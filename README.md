# GameDeploy Kiosk Application v1.2

A high-efficiency Windows application built with C# and WinUI 3, designed for game retail stores to rapidly copy multiple game installations onto customer-provided external drives.

## ?? **What's New in v1.2**

### **Real-Time USB Detection**
- ? **Instant USB drive detection** using Windows Management Instrumentation (WMI)
- ? **Live monitoring** - Drives appear/disappear automatically when plugged/unplugged
- ? **Visual indicators** - Green "LIVE" indicator shows real-time detection is active
- ? **USB identification** - Proper filtering to show only actual USB/removable drives
- ? **Enhanced UI** - Clear distinction between USB drives and demo drives

### **Improved User Experience**
- ? **Smart status messages** with emoji indicators for quick visual feedback
- ? **Real-time notifications** when USB drives are detected
- ? **USB drive icons** to clearly identify removable storage
- ? **Better error handling** with descriptive messages

---

## Features

### Core Functionality
- **Multi-Drive Management**: Automatically detects and manages multiple USB drives simultaneously
- **Centralized Game Library**: Scans and indexes games from a configurable library location
- **Parallel Copy Engine**: Performs concurrent copy operations to different drives for maximum throughput
- **Real-time Progress Tracking**: Individual and overall progress monitoring with detailed status updates
- **Robust Error Handling**: Isolated failure handling that doesn't affect other ongoing operations

### User Experience
- **Intuitive Kiosk Interface**: Clean, dark-themed UI optimized for fast-paced retail environments
- **Instant Search**: High-performance search filtering for large game libraries
- **Pre-deployment Validation**: Automatic space checking before starting copy operations
- **One-click Deployment**: Simple workflow to minimize training time
- **Live USB Detection**: Real-time monitoring of USB drive connections

### Technical Features
- **Async/Await Architecture**: Fully asynchronous operations prevent UI freezing
- **Thread-safe Operations**: Concurrent copy jobs with thread-safe progress reporting
- **WMI Integration**: Windows Management Instrumentation for real-time USB detection
- **Logging System**: Comprehensive deployment logging for store records
- **MVVM Pattern**: Clean separation of concerns with data binding

## System Requirements

- Windows 10 version 1809 (build 17763) or later
- .NET 9 runtime
- USB 3.0 ports for optimal performance
- Minimum 4GB RAM (8GB recommended for large deployments)
- Administrator privileges (for WMI USB detection)

## Installation

1. Download the MSIX package from the releases section
2. Install the package by double-clicking the .msix file
3. Launch "GameDeploy Kiosk" from the Start menu
4. **Important**: Run as Administrator for full USB detection capabilities

## Configuration

### Game Library Setup
1. Create a directory for your game library (default: `C:\GameLibrary`)
2. Organize games in individual folders within the library directory
3. Each game folder should contain the complete game installation files
4. The application will automatically scan and index the library on startup

### Library Structure ExampleC:\GameLibrary\
??? Cyberpunk 2077\
?   ??? bin\
?   ??? engine\
?   ??? Cyberpunk2077.exe
??? The Witcher 3\
?   ??? content\
?   ??? DLC\
?   ??? witcher3.exe
??? Elden Ring\
    ??? Game\
    ??? eldenring.exe
## Usage

### For Store Employees

1. **Connect Customer Drives**: Insert customer USB drives into available ports
   - **NEW**: Drives appear automatically within 1-2 seconds!
   - Green "LIVE" indicator confirms real-time detection is active
   - USB icon appears next to actual USB/removable drives

2. **Select Games**: Use the search box to find games, then check desired games
3. **Select Target Drives**: Check the drives where games should be copied
4. **Start Deployment**: Click "START DEPLOYMENT" to begin copying
5. **Monitor Progress**: Watch individual job progress and overall completion
6. **Handle Issues**: Failed jobs are clearly marked and don't affect other operations

### ?? **New Real-Time Features**

- **Automatic Detection**: No more manual refresh clicks - drives appear instantly
- **Live Status**: Green dot and "LIVE" text show the system is actively monitoring
- **Smart Notifications**: Status updates when drives are detected
- **USB Filtering**: Only shows actual USB drives (no more confusion with internal drives)

## Technical Architecture

### Project StructureGameCopier/
??? Models/                 # Data models (Game, Drive, DeploymentJob)
??? Services/              # Business logic services
?   ??? LibraryService.cs  # Game library management
?   ??? DriveService.cs    # Enhanced USB drive detection
?   ??? UsbDriveDetectionService.cs # NEW: Real-time WMI monitoring
?   ??? DeploymentService.cs # Parallel copy engine
?   ??? LoggingService.cs  # Operation logging
??? ViewModels/            # MVVM view models
??? Converters/            # XAML value converters
??? MainWindow.xaml        # Enhanced main UI
### Key Components

#### ?? UsbDriveDetectionService
- Real-time USB drive insertion/removal detection using WMI
- Windows Management event monitoring for immediate notifications
- Automatic drive list updates without user intervention

#### Enhanced DriveService
- WMI-based USB drive identification
- Proper filtering of actual USB/removable drives vs. internal drives
- Fallback detection methods for maximum compatibility

#### Improved MainViewModel
- Event-driven USB detection integration
- Enhanced status messaging with emoji indicators
- Smart drive list management with selection preservation

## Real-Time USB Detection

### How It Works
The application uses Windows Management Instrumentation (WMI) to monitor system events:
// Monitors USB drive insertions
Win32_VolumeChangeEvent WHERE EventType = 2

// Monitors USB drive removals  
Win32_VolumeChangeEvent WHERE EventType = 3
### Features
- **Instant Detection**: 1-2 second response time when drives are plugged in
- **Proper Filtering**: Only shows actual USB/removable drives
- **Selection Preservation**: Maintains drive selections during auto-refresh
- **Visual Feedback**: Live indicator and status messages
- **Fallback Support**: Works even if WMI permissions are limited

## Performance Optimization

### USB Detection Performance
- **Real-time events**: No polling delays - instant response to USB changes
- **Efficient filtering**: WMI queries target only relevant drives
- **Background processing**: Detection doesn't block the UI
- **Smart updates**: Only refreshes when actual changes occur

### Concurrent Operations
- Maximum 4 concurrent copy jobs by default (configurable)
- File-level parallelism within each copy job
- Optimized 80KB buffer size for file operations
- Thread pool management for UI responsiveness

## Troubleshooting

### USB Detection Issues

**USB drives not appearing automatically:**
- Ensure application is running as Administrator
- Check Windows Event Viewer for WMI access errors
- Try the "Manual Refresh" button as fallback
- Verify USB ports are working in Windows Explorer

**Only seeing local hard drives:**
- This indicates WMI USB detection failed
- Application falls back to demo mode for testing
- Restart as Administrator to enable full USB detection
- Check antivirus software isn't blocking WMI access

**Real-time detection stops working:**
- Click the settings button (??) to restart monitoring
- Check if Windows Management service is running
- Restart the application if issues persist

### Common Issues

**Games not appearing in library**
- Verify game folders exist in `C:\GameLibrary`
- Use "Refresh" button to rescan the library
- Check folder permissions

**Copy operation fails**
- Ensure sufficient space on target drive
- Verify drive is not write-protected
- Check USB connection stability

**Application freezes**
- All operations are asynchronous - freezing indicates a bug
- Restart the application
- Check Windows Event Viewer for errors

## Development

### Building from Sourcegit clone https://github.com/yourorg/GameDeploy.git
cd GameDeploy
dotnet restore
dotnet build --configuration Release
### Dependencies
- Microsoft.WindowsAppSDK (1.7.250606001)
- CommunityToolkit.Mvvm (8.4.0)
- System.Management (9.0.6) - **NEW for USB detection**
- Microsoft.Windows.SDK.BuildTools (10.0.26100.4188)

### New Features Added
- **UsbDriveDetectionService**: Real-time WMI-based USB monitoring
- **Enhanced DriveService**: Better USB drive identification and filtering
- **Improved UI**: Live indicators, USB icons, enhanced status messages
- **BoolToVisibilityConverter**: For conditional UI elements

## Deployment Notes

### Administrator Privileges
The application now requires Administrator privileges for full functionality:
- **With Admin**: Full real-time USB detection via WMI
- **Without Admin**: Falls back to polling-based detection

### Group Policy Considerations
Some enterprise environments may restrict WMI access:
- Ensure WMI services are enabled
- Check Group Policy settings for Management Instrumentation
- Consider adding application to WMI access whitelist

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For technical support or feature requests:
- Create an issue on GitHub
- Email: support@gamedeploy.com
- Documentation: https://docs.gamedeploy.com

---

**GameDeploy Kiosk v1.2** - Now with real-time USB detection for the ultimate retail efficiency!