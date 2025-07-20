# GameCopier

A modern WinUI 3 desktop application for Windows that enables fast, user-friendly deployment of game folders to USB drives and external storage devices.

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://github.com/microsoft/WindowsAppSDK)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## ? Features

### ?? Game Library Management
- **Multiple Game Directories**: Add and manage multiple game library folders
- **Automatic Scanning**: Automatically detects games in configured directories
- **Real-time Search**: Quick search functionality across your entire game library
- **Size Calculation**: Displays game sizes with automatic folder size calculation

### ?? Advanced Drive Detection
- **Smart Drive Recognition**: Detects USB drives, external hard drives, and network storage
- **Real-time Monitoring**: Automatic detection of newly connected drives with visual highlighting
- **Enhanced Drive Information**: Shows drive brands, models, file systems, and detailed capacity info
- **Customizable Filtering**: Show/hide different drive types (USB, Fixed, Network, CD/DVD, RAM)

### ?? Comprehensive Settings
- **Drive Type Filters**: Choose which types of drives to display
- **Individual Drive Hiding**: Hide specific drive letters from the main interface
- **Quick Actions**: Bulk show/hide operations for drive management
- **Persistent Configuration**: All settings saved between sessions

### ?? High-Performance Deployment
- **Bulk Operations**: Deploy multiple games to multiple drives simultaneously
- **Parallel Processing**: Multi-threaded copying for maximum speed
- **Progress Tracking**: Real-time progress indicators for each deployment job
- **Space Validation**: Automatic space checking before deployment starts

### ??? Modern User Interface
- **WinUI 3**: Native Windows 11-style interface with smooth animations
- **Responsive Design**: Adapts to different window sizes and screen resolutions
- **Dark/Light Theme**: Follows system theme preferences
- **Accessibility**: Full keyboard navigation and screen reader support

## ?? System Requirements

- **OS**: Windows 10 version 1809 (build 17763) or later
- **Runtime**: .NET 9.0 Runtime
- **Architecture**: x86, x64, or ARM64
- **Memory**: 512 MB RAM minimum, 1 GB recommended
- **Storage**: 100 MB free disk space

## ?? Installation

### Option 1: Download Release
1. Go to the [Releases](../../releases) page
2. Download the latest `.msix` package
3. Double-click to install

### Option 2: Build from Source# Clone the repository
git clone https://github.com/yourusername/GameCopier.git
cd GameCopier

# Restore dependencies
dotnet restore

# Build the application
dotnet build --configuration Release

# Run the application
dotnet run --project GameCopier/GameCopier/GameCopier.csproj
## ?? Quick Start Guide

### 1. Configure Game Libraries
1. Open **Settings** (gear icon in top-right)
2. In **Game Library Folders** section, click **Add Folder**
3. Select your game directories (e.g., Steam library, Epic Games, custom folders)
4. Click **Close** - games will be automatically scanned

### 2. Configure Drive Display
1. In **Settings** ? **Drive Display Settings**
2. Enable **Show Internal/Fixed Drives** to see USB hard drives
3. Optionally hide specific drive letters you don't want to see
4. Use quick actions: **Show All Drives** or **Hide All Non-System**

### 3. Deploy Games
1. **Select Games**: Check games you want to deploy from the left panel
2. **Select Drives**: Check target USB drives from the right panel
3. **Start Deployment**: Click the deploy button to begin copying
4. **Monitor Progress**: Watch real-time progress for each deployment job

## ??? Architecture

### Core Components
- **LibraryService**: Manages game discovery and library configuration
- **DriveService**: Handles drive detection, filtering, and information retrieval
- **DeploymentService**: Orchestrates multi-threaded game deployment operations
- **SettingsService**: Manages user preferences and configuration persistence
- **UsbDriveDetectionService**: Real-time USB drive monitoring and event handling

### Key Technologies
- **WinUI 3**: Modern Windows UI framework
- **MVVM Pattern**: Clean separation between UI and business logic
- **System.Management**: WMI integration for enhanced drive information
- **Parallel Processing**: Multi-threaded file operations for performance
- **JSON Configuration**: Lightweight settings persistence

## ??? Development

### Prerequisites
- **Visual Studio 2022** (17.8 or later) with:
  - .NET 9.0 SDK
  - Windows App SDK workload
  - C# 13.0 language features

### Project StructureGameCopier/
??? GameCopier/                 # Main application project
?   ??? Models/                 # Data models (Game, Drive, DeploymentJob)
?   ??? Services/               # Business logic services
?   ??? ViewModels/             # MVVM view models
?   ??? Views/                  # XAML views and dialogs
?   ??? MainWindow.xaml         # Main application window
??? README.md                   # This file
??? LICENSE                     # MIT license
??? .gitignore                  # Git ignore rules
### Building# Debug build
dotnet build

# Release build
dotnet build --configuration Release

# Create deployable package
dotnet publish --configuration Release --self-contained true
### Testing# Run unit tests (when available)
dotnet test

# Run with detailed logging
dotnet run --configuration Debug
## ?? Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Ways to Contribute
- ?? **Report Bugs**: Submit detailed bug reports with reproduction steps
- ?? **Suggest Features**: Propose new functionality or improvements
- ?? **Improve Documentation**: Help make our docs clearer and more comprehensive
- ?? **Submit Pull Requests**: Fix bugs or implement new features

### Development Workflow
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes with clear, descriptive commits
4. Add tests for new functionality
5. Submit a pull request with a detailed description

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ?? Acknowledgments

- **Microsoft**: For the excellent WinUI 3 framework and Windows App SDK
- **Community Contributors**: For bug reports, feature suggestions, and code contributions
- **.NET Foundation**: For the robust .NET ecosystem

## ?? Support

- **Issues**: [GitHub Issues](../../issues)
- **Discussions**: [GitHub Discussions](../../discussions)
- **Documentation**: [Wiki](../../wiki)

## ?? Changelog

See [CHANGELOG.md](CHANGELOG.md) for a detailed history of changes.

---

**Made with ?? for the gaming community**