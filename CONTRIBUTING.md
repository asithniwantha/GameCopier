# Contributing to GameCopier

Thank you for your interest in contributing to GameCopier! This document provides guidelines and information for contributors.

## ?? Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [How to Contribute](#how-to-contribute)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Issue Guidelines](#issue-guidelines)

## ?? Code of Conduct

This project follows a Code of Conduct to ensure a welcoming environment for all contributors. By participating, you agree to uphold this code.

### Our Standards

- **Be respectful** and inclusive in all interactions
- **Be constructive** when providing feedback
- **Focus on the issue**, not the person
- **Help create a positive environment** for learning and collaboration

## ?? Getting Started

### Prerequisites

Before contributing, ensure you have:

- **Visual Studio 2022** (17.8+) with:
  - .NET 9.0 SDK
  - Windows App SDK workload
  - Git integration
- **Git** installed and configured
- **Windows 10/11** development environment

### Development Setup

1. **Fork and Clone**
   ```bash
   git clone https://github.com/yourusername/GameCopier.git
   cd GameCopier
   ```

2. **Create a Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Build and Test**
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project GameCopier/GameCopier/GameCopier.csproj
   ```

## ?? How to Contribute

### Types of Contributions

1. **?? Bug Reports**
   - Use the bug report template
   - Include reproduction steps
   - Provide system information
   - Attach logs or screenshots

2. **?? Feature Requests**
   - Use the feature request template
   - Explain the use case
   - Describe the expected behavior
   - Consider implementation impact

3. **?? Documentation**
   - Improve README files
   - Add code comments
   - Create wiki pages
   - Fix typos and grammar

4. **?? Code Contributions**
   - Fix bugs
   - Implement new features
   - Improve performance
   - Refactor code

### Areas That Need Help

- **Drive Detection**: Improving USB drive recognition across different hardware
- **Performance**: Optimizing file copy operations and UI responsiveness
- **Accessibility**: Enhancing keyboard navigation and screen reader support
- **Localization**: Adding support for additional languages
- **Testing**: Creating comprehensive unit and integration tests

## ?? Coding Standards

### C# Style Guidelines

We follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions):

#### Naming Conventions
```csharp
// Classes and Methods: PascalCase
public class DriveService { }
public void LoadDrivesAsync() { }

// Fields: camelCase with underscore prefix
private readonly LibraryService _libraryService;

// Properties: PascalCase
public string DriveLetter { get; set; }

// Constants: PascalCase
public const string DefaultLibraryPath = "C:\\GameLibrary";
```

#### Code Organization
```csharp
// 1. Using statements (grouped and sorted)
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

// 2. Namespace
namespace GameCopier.Services
{
    // 3. Class with XML documentation
    /// <summary>
    /// Provides services for managing game libraries.
    /// </summary>
    public class LibraryService
    {
        // 4. Fields
        private readonly string _configPath;
        
        // 5. Properties
        public IEnumerable<Game> Games { get; }
        
        // 6. Constructor
        public LibraryService() { }
        
        // 7. Public methods
        public async Task<IEnumerable<Game>> LoadGamesAsync() { }
        
        // 8. Private methods
        private void SaveConfiguration() { }
    }
}
```

#### XAML Guidelines
```xml
<!-- Use proper indentation -->
<StackPanel Orientation="Vertical" 
            Spacing="8"
            Margin="16">
    <!-- Meaningful x:Name attributes -->
    <TextBlock x:Name="HeaderTextBlock"
               Text="Game Library"
               Style="{StaticResource HeaderTextBlockStyle}" />
               
    <!-- Consistent property ordering: Layout, Appearance, Behavior -->
    <Button Grid.Row="0"
            Grid.Column="1" 
            Content="Add Folder"
            Style="{StaticResource AccentButtonStyle}"
            Command="{Binding AddFolderCommand}" />
</StackPanel>
```

### Architecture Patterns

#### MVVM Implementation
```csharp
// ViewModels should inherit from INotifyPropertyChanged
public class MainViewModel : INotifyPropertyChanged
{
    // Use ObservableCollection for collections
    public ObservableCollection<Game> Games { get; } = new();
    
    // Commands should use ICommand interface
    public ICommand LoadGamesCommand { get; }
    
    // Proper property change notification
    private string _statusText = "";
    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }
}
```

#### Service Layer
```csharp
// Services should be stateless or manage their own state
public class DriveService
{
    // Use dependency injection where appropriate
    private readonly SettingsService _settingsService;
    
    // Async methods should be properly named
    public async Task<IEnumerable<Drive>> GetDrivesAsync()
    {
        // Use ConfigureAwait(false) for library code
        var drives = await DetectDrivesAsync().ConfigureAwait(false);
        return drives;
    }
}
```

## ?? Commit Guidelines

### Commit Message Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

#### Types
- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation only changes
- **style**: Code style changes (formatting, missing semicolons, etc.)
- **refactor**: Code refactoring without feature changes
- **perf**: Performance improvements
- **test**: Adding or updating tests
- **chore**: Maintenance tasks, dependency updates

#### Examples
```bash
feat(drives): add support for network drive detection

- Implement network drive discovery using WMI
- Add filtering options for network drives
- Update UI to display network drive status

Closes #123

fix(ui): resolve drive list not updating after USB insertion

The drive list was not refreshing automatically when new USB drives
were inserted due to missing event handler registration.

- Register USB insertion event handler on startup
- Ensure UI updates occur on main thread
- Add debugging logs for drive detection events

Fixes #456
```

## ?? Pull Request Process

### Before Submitting
1. **Test thoroughly** on your local machine
2. **Run the full build** to ensure no compilation errors
3. **Update documentation** if you've changed functionality
4. **Add tests** for new features (when test framework is available)
5. **Check code style** compliance

### PR Template
```markdown
## Description
Brief description of changes and motivation.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Testing
- [ ] Tested on Windows 10
- [ ] Tested on Windows 11
- [ ] Tested with multiple drive types
- [ ] Tested with large game libraries

## Screenshots (if applicable)
Add screenshots to help explain your changes.

## Checklist
- [ ] My code follows the style guidelines
- [ ] I have performed a self-review of my code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation
```

### Review Process
1. **Automated checks** must pass (build, style, tests)
2. **Code review** by at least one maintainer
3. **Testing** by reviewers when necessary
4. **Final approval** and merge by maintainer

## ?? Issue Guidelines

### Bug Reports
Use the bug report template and include:

- **GameCopier version**
- **Windows version** and build number
- **Hardware information** (especially for drive detection issues)
- **Steps to reproduce**
- **Expected vs actual behavior**
- **Error messages or logs**
- **Screenshots or videos** if helpful

### Feature Requests
Use the feature request template and include:

- **Problem description**: What problem does this solve?
- **Proposed solution**: How should it work?
- **Alternatives considered**: Other approaches you've thought of
- **Additional context**: Mockups, examples, related issues

### Questions and Discussions
For questions about usage, development, or general discussion:
- Use [GitHub Discussions](../../discussions)
- Search existing discussions first
- Use appropriate categories
- Provide context and examples

## ??? Issue Labels

We use labels to categorize issues:

### Type Labels
- `bug`: Something isn't working
- `enhancement`: New feature or request
- `documentation`: Improvements to documentation
- `question`: Further information is requested

### Priority Labels
- `priority/critical`: Critical issues that block functionality
- `priority/high`: Important issues that should be addressed soon
- `priority/medium`: Standard priority issues
- `priority/low`: Nice-to-have improvements

### Area Labels
- `area/ui`: User interface and UX
- `area/drives`: Drive detection and management
- `area/deployment`: Game copying and deployment
- `area/settings`: Configuration and preferences
- `area/performance`: Performance improvements

### Status Labels
- `status/needs-triage`: Needs initial review
- `status/in-progress`: Currently being worked on
- `status/blocked`: Cannot proceed due to dependencies
- `status/needs-info`: Waiting for additional information

## ?? Recognition

Contributors will be:
- **Listed in the README** contributors section
- **Mentioned in release notes** for significant contributions
- **Invited to the contributors team** for ongoing contributors

## ?? Getting Help

If you need help contributing:
- **Create a discussion** for general questions
- **Join our community** channels (when available)
- **Reach out to maintainers** through GitHub

Thank you for contributing to GameCopier! ??