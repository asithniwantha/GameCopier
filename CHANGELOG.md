# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project setup with WinUI 3 framework
- Comprehensive drive detection and filtering system
- Individual drive letter hiding functionality
- Enhanced settings dialog with scrollable interface
- Real-time USB drive monitoring with visual highlighting
- Multi-threaded game deployment with progress tracking
- Game library management with multiple folder support
- Advanced drive information display (brand, model, filesystem)

### Features
- **Game Library Management**
  - Multiple game directory support
  - Automatic game scanning and size calculation
  - Real-time search functionality across game collections
  - Add/remove game folders through settings interface

- **Drive Management**
  - Smart USB and external drive detection
  - Support for all Windows drive types (Fixed, Removable, Network, CD/DVD, RAM)
  - Real-time drive insertion/removal monitoring
  - Enhanced drive information with WMI integration
  - Customizable drive type filtering
  - Individual drive letter hiding/showing
  - Quick actions for bulk drive visibility management

- **Deployment System**
  - Multi-threaded parallel file copying
  - Real-time progress tracking for each deployment job
  - Automatic space validation before deployment
  - Support for deploying multiple games to multiple drives
  - Cancellation support for ongoing deployments

- **Settings & Configuration**
  - Persistent settings storage using JSON configuration
  - Comprehensive drive display customization
  - Game library folder management
  - System drive hiding option
  - Import/export configuration capabilities

- **User Interface**
  - Modern WinUI 3 design with native Windows 11 styling
  - Responsive layout adapting to different window sizes
  - Real-time status updates and progress indicators
  - Accessible design with keyboard navigation support
  - Dark/light theme support following system preferences

### Technical Highlights
- **Architecture**: Clean MVVM pattern implementation
- **Performance**: Optimized file operations with parallel processing
- **Reliability**: Comprehensive error handling and logging
- **Compatibility**: Support for Windows 10 1809+ and Windows 11
- **Scalability**: Efficient handling of large game libraries and multiple drives

### Dependencies
- .NET 9.0 Runtime
- Microsoft Windows App SDK 1.7+
- CommunityToolkit.Mvvm 8.4.0
- System.Management 9.0.6

## [0.1.0] - 2024-12-XX

### Added
- Initial release of GameCopier
- Core functionality for game library management
- Basic drive detection and deployment features
- Settings dialog with drive filtering options
- Real-time USB drive monitoring
- Multi-threaded deployment system

---

## Release Notes Template

When creating new releases, use this template:

```markdown
## [X.Y.Z] - YYYY-MM-DD

### Added
- New features and functionality

### Changed
- Changes to existing functionality

### Deprecated
- Soon-to-be removed features

### Removed
- Features removed in this version

### Fixed
- Bug fixes

### Security
- Security improvements and fixes
```

### Version Numbering

This project uses [Semantic Versioning](https://semver.org/):

- **MAJOR** version when you make incompatible API changes
- **MINOR** version when you add functionality in a backwards compatible manner
- **PATCH** version when you make backwards compatible bug fixes

Additional labels for pre-release and build metadata are available as extensions to the MAJOR.MINOR.PATCH format.