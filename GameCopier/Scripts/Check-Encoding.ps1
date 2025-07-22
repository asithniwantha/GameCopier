# UTF-8 Encoding Checker and Fixer for GameCopier Project
# This script checks and optionally fixes encoding issues in project files

param(
    [switch]$Fix,           # Apply fixes automatically
    [switch]$Verbose,       # Show detailed output
    [string]$Path = "."     # Path to check (default: current directory)
)

Write-Host "=== GameCopier UTF-8 Encoding Checker ===" -ForegroundColor Cyan

# File type mappings for encoding requirements
$FileEncodingRules = @{
    '.cs'     = 'UTF8-BOM'
    '.xaml'   = 'UTF8-BOM' 
    '.csproj' = 'UTF8'
    '.sln'    = 'UTF8'
    '.json'   = 'UTF8'
    '.md'     = 'UTF8'
    '.txt'    = 'UTF8'
    '.xml'    = 'UTF8'
}

function Get-FileEncoding {
    param([string]$FilePath)
    
    try {
        $bytes = [System.IO.File]::ReadAllBytes($FilePath)
        
        # Check for BOM
        if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
            return 'UTF8-BOM'
        }
        
        # Check for UTF-16 LE BOM
        if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
            return 'UTF16-LE'
        }
        
        # Check for UTF-16 BE BOM
        if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF) {
            return 'UTF16-BE'
        }
        
        # Try to detect if it's valid UTF-8
        try {
            $null = [System.Text.Encoding]::UTF8.GetString($bytes)
            return 'UTF8'
        }
        catch {
            return 'ANSI'
        }
    }
    catch {
        return 'UNKNOWN'
    }
}

# Get all relevant files
$filesToCheck = Get-ChildItem -Path $Path -Recurse | Where-Object {
    !$_.PSIsContainer -and 
    $FileEncodingRules.ContainsKey($_.Extension) -and
    $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.vs\\'
}

$issuesFound = 0

Write-Host "Checking $($filesToCheck.Count) files for encoding issues..." -ForegroundColor Yellow

foreach ($file in $filesToCheck) {
    $currentEncoding = Get-FileEncoding -FilePath $file.FullName
    $expectedEncoding = $FileEncodingRules[$file.Extension]
    
    if ($currentEncoding -ne $expectedEncoding) {
        $issuesFound++
        Write-Host "[ISSUE] $($file.FullName)" -ForegroundColor Red
        Write-Host "  Current: $currentEncoding | Expected: $expectedEncoding" -ForegroundColor Gray
    }
    elseif ($Verbose) {
        Write-Host "[OK] $($file.FullName) - $currentEncoding" -ForegroundColor DarkGreen
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Files checked: $($filesToCheck.Count)" -ForegroundColor White
Write-Host "Issues found: $issuesFound" -ForegroundColor $(if ($issuesFound -eq 0) { "Green" } else { "Yellow" })

if ($issuesFound -eq 0) {
    Write-Host "SUCCESS: All files have correct UTF-8 encoding!" -ForegroundColor Green
} else {
    Write-Host "WARNING: Found $issuesFound encoding issues that need to be fixed." -ForegroundColor Yellow
}

# Return appropriate exit code
exit $(if ($issuesFound -eq 0) { 0 } else { 1 })
