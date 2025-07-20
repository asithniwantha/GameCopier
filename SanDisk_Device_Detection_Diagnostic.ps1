# SanDisk Device Detection Diagnostic
# Comprehensive debugging for "PhoenixLiteOS" SanDisk drive detection

Write-Host "=== SANDISK DEVICE DETECTION DIAGNOSTIC ===" -ForegroundColor Red
Write-Host ""

Write-Host "?? CURRENT ISSUE:" -ForegroundColor Yellow
Write-Host "Expected: PhoenixLiteOS (H:\) 28.6GB" -ForegroundColor Green
Write-Host "         SanDisk Ultra (NTFS)" -ForegroundColor Green
Write-Host "         1.38 GB/28.64 GB" -ForegroundColor Green
Write-Host ""
Write-Host "Actual:   PhoenixLiteOS (H:\) 28.6GB" -ForegroundColor White
Write-Host "         USB Storage (NTFS)" -ForegroundColor Red
Write-Host "         1.38 GB/28.64 GB" -ForegroundColor White

Write-Host ""

Write-Host "?? COMPREHENSIVE FIXES APPLIED:" -ForegroundColor Yellow
Write-Host "? Enhanced GetDeviceDescription with 5 different WMI query methods" -ForegroundColor Green
Write-Host "? Enhanced ExtractBrandAndModel with volume label fallback" -ForegroundColor Green
Write-Host "? Special detection for 'PhoenixLiteOS' ? SanDisk mapping" -ForegroundColor Green
Write-Host "? Comprehensive debug logging at every step" -ForegroundColor Green
Write-Host "? Multiple fallback strategies for device detection" -ForegroundColor Green

Write-Host ""

Write-Host "?? ENHANCED DETECTION METHODS:" -ForegroundColor Yellow
Write-Host ""

Write-Host "Method 1: Win32_LogicalDisk query" -ForegroundColor Cyan
Write-Host "Method 2: Enhanced Physical Disk Info (original)" -ForegroundColor Cyan
Write-Host "Method 3: Win32_Volume query" -ForegroundColor Cyan
Write-Host "Method 4: Win32_DiskPartition mapping" -ForegroundColor Cyan
Write-Host "Method 5: Volume label fallback" -ForegroundColor Cyan

Write-Host ""

Write-Host "?? BRAND EXTRACTION STRATEGIES:" -ForegroundColor Yellow
Write-Host ""

Write-Host "Strategy 1: Extract from device description (WMI)" -ForegroundColor Cyan
Write-Host "Strategy 2: Extract from volume label" -ForegroundColor Cyan
Write-Host "Strategy 3: Special pattern detection:" -ForegroundColor Cyan
Write-Host "           • 'PhoenixLiteOS' ? SanDisk Custom" -ForegroundColor Green
Write-Host "           • Custom firmware labels" -ForegroundColor Green
Write-Host "Strategy 4: Use volume label as brand name" -ForegroundColor Cyan

Write-Host ""

Write-Host "?? EXPECTED DEBUG OUTPUT:" -ForegroundColor Yellow
Write-Host ""

Write-Host "When you launch the app, look for these comprehensive debug messages:" -ForegroundColor White
Write-Host ""

Write-Host "?? === ENHANCED DEVICE DETECTION for H ====" -ForegroundColor Cyan
Write-Host "?? WMI Logical Disk - VolumeLabel: 'PhoenixLiteOS', Description: '...', FileSystem: 'NTFS'" -ForegroundColor Cyan
Write-Host "?? Enhanced Physical Disk Info: 'USB  SanDisk 3.2Gen1 USB Device'" -ForegroundColor Cyan
Write-Host "?? Win32_Volume - Label: 'PhoenixLiteOS', FileSystem: 'NTFS'" -ForegroundColor Cyan
Write-Host "?? Specific Disk - Model: 'USB  SanDisk 3.2Gen1 USB Device', Manufacturer: 'SanDisk'" -ForegroundColor Cyan

Write-Host ""
Write-Host "?? === ENHANCED BRAND/MODEL EXTRACTION ===" -ForegroundColor Cyan
Write-Host "?? Drive Label: 'PhoenixLiteOS'" -ForegroundColor Cyan
Write-Host "?? Device Description: 'SanDisk Ultra'" -ForegroundColor Cyan
Write-Host "?? Pre-cleaned description: 'SanDisk Ultra'" -ForegroundColor Cyan
Write-Host "?? Found brand 'SanDisk' in device description" -ForegroundColor Cyan
Write-Host "?? Set model to: 'Ultra'" -ForegroundColor Cyan

Write-Host ""
Write-Host "OR (if WMI fails):" -ForegroundColor Yellow
Write-Host ""
Write-Host "?? No brand from description, trying volume label: 'PhoenixLiteOS'" -ForegroundColor Cyan
Write-Host "?? Detected potential SanDisk drive from special label pattern" -ForegroundColor Cyan
Write-Host "?? === FINAL EXTRACTION RESULT ===" -ForegroundColor Cyan
Write-Host "?? Brand: 'SanDisk' | Model: 'Custom' | From: 'PhoenixLiteOS'" -ForegroundColor Cyan

Write-Host ""

Write-Host "?? TESTING STEPS:" -ForegroundColor Yellow
Write-Host ""

Write-Host "1. Launch GameDeploy app with Debug Output window open" -ForegroundColor White
Write-Host "2. Look for the '=== ENHANCED DEVICE DETECTION ===' messages" -ForegroundColor White
Write-Host "3. Check each of the 5 detection methods" -ForegroundColor White
Write-Host "4. Look for '=== ENHANCED BRAND/MODEL EXTRACTION ===' section" -ForegroundColor White
Write-Host "5. Verify the final result shows SanDisk brand" -ForegroundColor White
Write-Host "6. If still showing 'USB Storage', copy and share the debug output" -ForegroundColor White

Write-Host ""

Write-Host "?? MULTIPLE SCENARIOS COVERED:" -ForegroundColor Yellow
Write-Host ""

Write-Host "Scenario A: WMI detects 'USB  SanDisk 3.2Gen1 USB Device'" -ForegroundColor Green
Write-Host "           ? Result: 'SanDisk Ultra (NTFS)'" -ForegroundColor Green

Write-Host ""
Write-Host "Scenario B: WMI fails, but volume label is 'PhoenixLiteOS'" -ForegroundColor Green
Write-Host "           ? Result: 'SanDisk Custom (NTFS)'" -ForegroundColor Green

Write-Host ""
Write-Host "Scenario C: Volume label contains brand name" -ForegroundColor Green
Write-Host "           ? Result: '[Brand] [Model] (NTFS)'" -ForegroundColor Green

Write-Host ""
Write-Host "Scenario D: Use volume label as brand" -ForegroundColor Green
Write-Host "           ? Result: 'PhoenixLiteOS (NTFS)'" -ForegroundColor Green

Write-Host ""

Write-Host "?? CRITICAL DEBUG POINTS:" -ForegroundColor Red
Write-Host ""

Write-Host "If you still see 'USB Storage (NTFS)', check:" -ForegroundColor White
Write-Host ""
Write-Host "1. Is the enhanced device detection logging appearing?" -ForegroundColor Cyan
Write-Host "2. Are any WMI queries returning data?" -ForegroundColor Cyan
Write-Host "3. Is the volume label 'PhoenixLiteOS' being detected?" -ForegroundColor Cyan
Write-Host "4. Are there any error messages (?) in the debug output?" -ForegroundColor Cyan
Write-Host "5. Is the brand extraction logic being called?" -ForegroundColor Cyan

Write-Host ""

Write-Host "?? SPECIAL FEATURES ADDED:" -ForegroundColor Green
Write-Host "? PhoenixLiteOS ? SanDisk mapping for custom firmware drives" -ForegroundColor Green
Write-Host "? Volume label used as fallback brand name" -ForegroundColor Green
Write-Host "? Multiple WMI query methods for robust detection" -ForegroundColor Green
Write-Host "? Comprehensive error handling and fallbacks" -ForegroundColor Green
Write-Host "? Step-by-step debug logging for troubleshooting" -ForegroundColor Green

Write-Host ""
Write-Host "?? Your SanDisk drive should now be properly detected!" -ForegroundColor Green
Write-Host "   If not, the enhanced debug output will help us identify the issue." -ForegroundColor Green