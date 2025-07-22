# Changelog

All notable changes to GameCopier will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned for v2.0.0
- Parallel multi-drive copy operations
- Real-time queue additions during active operations
- Advanced progress tracking with ETA calculations
- Smart copy optimization with deduplication
- Resource management and bandwidth control
- Cloud integration capabilities

## [1.0.0-preview] - 2024-12-19

### Added
- Initial release of GameCopier
- Game and software library management
- USB drive detection and management
- Copy queue system with job tracking
- Multiple copy methods (Robocopy, Windows Explorer, Custom)
- Dual progress tracking (visible robocopy window + UI progress bars)
- Modern WinUI 3 interface with Fluent Design
- Settings dialog for library folder management
- Search functionality for games and software
- Real-time status updates and notifications
- Comprehensive error handling and logging
- Version information service
- Detailed documentation and roadmap

### Features
- **Library Management**: Add and manage game/software folders
- **Drive Detection**: Automatic USB drive discovery and monitoring
- **Copy Operations**: Queue-based copying with multiple method support
- **Progress Tracking**: Real-time progress bars and robocopy window display
- **Search & Filter**: Quick search across game and software libraries
- **Settings**: Configurable library paths and display options
- **Testing**: Built-in test functions for copy operation verification

### Technical Details
- Built with .NET 9.0 and WinUI 3
- MVVM architecture using CommunityToolkit.Mvvm
- JSON-based configuration storage
- Windows Shell API integration
- Robocopy integration for reliable file operations
- Thread-safe UI updates with proper async patterns

### Limitations (Addressed in v2.0.0)
- Sequential copy operations only
- Cannot add items to queue during active operations
- Basic progress tracking (enhanced tracking planned)
- Single-drive copy operations (parallel operations planned)

## [0.9.0-beta] - Development Phase

### Added
- Core application framework
- Basic UI layout and navigation
- Game/software detection algorithms
- USB drive monitoring service
- Initial copy operation implementation

### Fixed
- Memory leaks in drive monitoring
- UI threading issues
- File path handling edge cases

## [0.5.0-alpha] - Prototype Phase

### Added
- Proof of concept implementation
- Basic file copying functionality
- Simple UI mockups
- Core data models

---

## Version Numbering Scheme

GameCopier follows [Semantic Versioning](https://semver.org/):

- **MAJOR.MINOR.PATCH** format
- **MAJOR**: Incompatible API changes or major feature additions
- **MINOR**: Backwards-compatible functionality additions
- **PATCH**: Backwards-compatible bug fixes

### Pre-release Identifiers
- **alpha**: Early development, features incomplete
- **beta**: Feature-complete, testing phase
- **preview**: Release candidate, final testing
- **rc**: Release candidate, minimal changes expected

### Examples
- `1.0.0` - First stable release
- `1.1.0` - Minor feature addition
- `1.1.1` - Bug fix release
- `2.0.0-preview` - Major version preview
- `2.0.0-rc.1` - Release candidate

## Release Schedule

### Current Development Cycle
- **v1.0.0**: Initial stable release ✅
- **v1.0.x**: Bug fixes and minor improvements
- **v1.1.0**: Enhanced UI and user experience improvements
- **v2.0.0**: Parallel operations and advanced features

### Planned Release Timeline
- **Q1 2024**: v1.0.x maintenance releases
- **Q2 2024**: v2.0.0 with parallel operations
- **Q3 2024**: v2.1.0 with cloud integration
- **Q4 2024**: v3.0.0 with enterprise features

## Support Policy

### Long-Term Support (LTS)
- **v1.0.x**: Supported until v2.0.0 stable release
- **v2.0.x**: Will become LTS after v3.0.0 release
- **Security Updates**: Provided for current and previous major versions

### End of Life (EOL)
- **Alpha/Beta Versions**: No ongoing support
- **Previous Major Versions**: 6 months after new major release
- **Current Version**: Ongoing support and updates

---

*For more information about releases, see the [Releases page](https://github.com/yourusername/gamecopier/releases)*