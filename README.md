# 🎮 GameCopier

**Game and Software Deployment Tool with Multi-Drive Support**

[![Version](https://img.shields.io/badge/version-1.0.0--preview-blue.svg)](https://github.com/yourusername/gamecopier/releases)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Windows](https://img.shields.io/badge/platform-Windows%2010%2B-blue.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## 📋 Overview

GameCopier is a modern Windows application designed for efficiently copying games and software to multiple USB drives simultaneously. Built with WinUI 3 and .NET 9, it provides a clean, intuitive interface for managing large-scale game deployments.

## ✨ Key Features

### Current Features (v1.0.0)
- 🎯 **Dual Progress Tracking** - Visible robocopy window + real-time UI progress bars
- 🗂️ **Game & Software Library Management** - Organize your collections with folder monitoring
- 💾 **USB Drive Detection** - Automatic detection and management of connected drives
- 📊 **Copy Queue Management** - Queue multiple copy operations with status tracking
- 🎨 **Modern Windows 11 UI** - Clean, responsive interface with Fluent Design
- 🔧 **Multiple Copy Methods** - Robocopy, Windows Explorer, and custom implementations

### 🚀 Coming Soon (v2.0.0)
- ⚡ **Parallel Multi-Drive Copy** - Simultaneous copying to different drives
- 📈 **Real-Time Queue Additions** - Add items while copy operations are running
- 🎯 **Smart Copy Optimization** - Deduplication and incremental updates
- 📊 **Advanced Analytics** - Transfer speeds, ETAs, and performance metrics
- ☁️ **Cloud Integration** - OneDrive, Google Drive backup support

## 🚀 Quick Start

### Prerequisites
- Windows 10 version 1903 (19H1) or later
- .NET 9.0 Runtime
- At least 100MB free disk space

### Installation

1. **Download the latest release** from the [Releases page](https://github.com/yourusername/gamecopier/releases)
2. **Extract** the ZIP file to your desired location
3. **Run** `GameCopier.exe`

### First-Time Setup

1. **Add Game Folders**: Click the settings button (⚙️) and add your game library folders
2. **Connect USB Drives**: Plug in your USB drives - they'll appear automatically
3. **Start Copying**: Select games, choose target drives, and click "ADD & COPY"

## 🎯 Usage

### Basic Copy Operation

1. **Select Items**: Choose games or software from the library tabs
2. **Choose Drives**: Select target USB drives from the drives panel
3. **Add to Queue**: Click "ADD & COPY" to start the operation
4. **Monitor Progress**: Watch real-time progress in both the UI and robocopy window

### Advanced Features

- **Search**: Use the search boxes to quickly find specific games or software
- **Queue Management**: Monitor all copy operations in the queue panel
- **Settings**: Configure library folders and display preferences
- **Test Functions**: Use "TEST PROGRESS" to verify copy functionality

## 🛠️ Development

### Building from Source
# Clone the repository
git clone https://github.com/yourusername/gamecopier.git
cd gamecopier

# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Run the application
dotnet run --project GameCopier/GameCopier
### Project Structure
GameCopier/
├── GameCopier/                 # Main application project
│   ├── Views/                  # XAML views and UI
│   ├── ViewModels/            # MVVM view models
│   ├── Services/              # Business logic and data services
│   ├── Models/                # Data models and entities
│   ├── Converters/            # Value converters for UI
│   └── Core/                  # Core interfaces and utilities
├── Documentation/             # Project documentation
└── README.md                 # This file
### Technologies Used

- **Framework**: .NET 9.0 with WinUI 3
- **Architecture**: MVVM with CommunityToolkit.Mvvm
- **UI**: Windows App SDK 1.7+ with Fluent Design
- **File Operations**: Robocopy, Windows Shell APIs
- **Configuration**: JSON-based settings storage

## 📊 Performance

### Current Performance (v1.0.0)
- **Copy Speed**: Limited by sequential operations
- **UI Responsiveness**: Real-time progress updates
- **Memory Usage**: ~50-100MB during operations
- **Compatibility**: Windows 10 1903+ and Windows 11

### Planned Improvements (v2.0.0)
- **40-60% faster** copy times with parallel operations
- **Sub-second** response time for queue additions
- **99.9% reliability** for concurrent operations
- **Advanced resource management** and bandwidth control

## 🗺️ Roadmap

### Phase 1: Core Parallel Operations (Q1 2024)
- [ ] Parallel multi-drive copy implementation
- [ ] Real-time queue management during operations
- [ ] Enhanced progress tracking and metrics
- [ ] Resource management and optimization

### Phase 2: Advanced Features (Q2 2024)
- [ ] Smart copy optimization with deduplication
- [ ] Advanced analytics and performance metrics
- [ ] Enhanced error recovery mechanisms
- [ ] Plugin architecture foundation

### Phase 3: Enterprise Features (Q3 2024)
- [ ] Cloud integration (OneDrive, Google Drive)
- [ ] Network drive support
- [ ] Database integration for metadata
- [ ] Advanced reporting and logging

See [FUTURE_IMPROVEMENTS.md](FUTURE_IMPROVEMENTS.md) for detailed feature planning.

## 🤝 Contributing

We welcome contributions! Here's how you can help:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`