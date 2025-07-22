# UTF-8 Encoding Guidelines for GameCopier Project

## Overview
This document outlines the encoding standards and best practices for the GameCopier project to ensure consistent UTF-8 encoding across all files.

## File Encoding Standards

### All Files Must Use UTF-8 Encoding
- **Source files (.cs)**: UTF-8 with BOM
- **XAML files (.xaml)**: UTF-8 with BOM
- **Configuration files (.json, .xml, .config)**: UTF-8 without BOM
- **Documentation files (.md, .txt)**: UTF-8 without BOM
- **Project files (.csproj, .sln)**: UTF-8 without BOM

## Visual Studio Settings

### 1. Global Document Settings
1. Go to **Tools > Options > Environment > Documents**
2. Check **"Save documents as Unicode"**
3. Check **"Detect UTF-8 encoding without signature"**

### 2. Advanced Save Options (Per File)
- For new files: **File > Advanced Save Options**
- Select: **"Unicode (UTF-8 with signature) - Codepage 65001"** for .cs and .xaml files
- Select: **"Unicode (UTF-8 without signature) - Codepage 65001"** for .md, .json, .txt files

### 3. Default Encoding for New Files
1. **Tools > Options > Text Editor > File Extension**
2. Set default editor for each file type
3. Encoding will follow .editorconfig settings

## EditorConfig Configuration
The project includes a `.editorconfig` file that enforces UTF-8 encoding:

```ini
# All files
[*]
charset = utf-8

# Code files
[*.{cs,csx,vb,vbx}]
charset = utf-8-bom

# XAML files
[*.{xaml,xamlx}]
charset = utf-8

# Markdown and text files
[*.{md,txt,json}]
charset = utf-8
```

## Best Practices

### 1. Before Creating New Files
- Ensure Visual Studio is configured for UTF-8 encoding
- Check that .editorconfig is present in the project root

### 2. When Adding Existing Files
- Always check encoding using **File > Advanced Save Options**
- Re-save with correct UTF-8 encoding if needed

### 3. For Unicode Characters
- Use proper Unicode characters (like box-drawing: ├, │, └, ┬)
- Avoid copy-pasting from sources with different encodings
- Test display in different environments

### 4. Git Integration
- Configure Git to handle UTF-8 properly:
  ```bash
  git config core.quotepath false
  git config core.precomposeUnicode true
  ```

## Troubleshooting Encoding Issues

### Issue: Special Characters Display as `?`
**Cause**: File saved with wrong encoding or BOM issues
**Solution**: 
1. Open file in Visual Studio
2. **File > Advanced Save Options**
3. Select appropriate UTF-8 encoding
4. Save file

### Issue: Box-drawing Characters Corrupted
**Cause**: Copy-paste from incompatible source
**Solution**:
1. Use proper Unicode box-drawing characters:
   - `├` (U+251C) for tree branches
   - `│` (U+2502) for vertical lines
   - `└` (U+2514) for end branches
   - `┬` (U+252C) for top branches

### Issue: New Files Default to ANSI
**Cause**: Visual Studio not configured for UTF-8
**Solution**:
1. Check **Tools > Options > Environment > Documents**
2. Ensure **"Save documents as Unicode"** is checked
3. Verify .editorconfig is working

## File Type Specific Guidelines

### C# Source Files (.cs)
- **Encoding**: UTF-8 with BOM
- **Line Endings**: CRLF (Windows)
- **Indentation**: 4 spaces

### XAML Files (.xaml)
- **Encoding**: UTF-8 with BOM
- **XML Declaration**: `<?xml version="1.0" encoding="utf-8"?>`
- **Indentation**: 4 spaces

### JSON Configuration Files (.json)
- **Encoding**: UTF-8 without BOM
- **Indentation**: 2 spaces
- **Line Endings**: CRLF

### Markdown Documentation (.md)
- **Encoding**: UTF-8 without BOM
- **Line Endings**: CRLF
- **Indentation**: 2 spaces for lists

## Team Workflow

### 1. Setup Checklist for New Developers
- [ ] Configure Visual Studio UTF-8 settings
- [ ] Verify .editorconfig is recognized
- [ ] Test creating new files with correct encoding
- [ ] Configure Git for UTF-8 handling

### 2. Code Review Checklist
- [ ] All new files use UTF-8 encoding
- [ ] No encoding-related display issues
- [ ] Special characters render correctly
- [ ] .editorconfig compliance

### 3. Build Process
- The build process will validate encoding through .editorconfig
- CI/CD should check for encoding consistency
- Any encoding warnings should be treated as errors

## References
- [.NET Globalization and Localization](https://docs.microsoft.com/en-us/dotnet/standard/globalization-localization/)
- [EditorConfig Specification](https://editorconfig.org/)
- [Unicode in Visual Studio](https://docs.microsoft.com/en-us/visualstudio/ide/globalization-and-localization-of-applications)
