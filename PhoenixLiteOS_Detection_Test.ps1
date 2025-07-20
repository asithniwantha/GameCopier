# PhoenixLiteOS Drive Detection Test
# Specific test for the H:\ PhoenixLiteOS USB drive that should be detected

Write-Host "=== PHOENIXLITEOS DRIVE DETECTION TEST ===" -ForegroundColor Green
Write-Host ""

Write-Host "?? TARGET DRIVE: PhoenixLiteOS (H:\) 28.6 GB - Removable" -ForegroundColor Yellow
Write-Host ""

# Check if the specific drive is available
$phoenixDrive = Get-WmiObject -Class Win32_LogicalDisk | Where-Object { $_.DeviceID -eq "H:" -and $_.DriveType -eq 2 }

if ($phoenixDrive) {
    Write-Host "? PhoenixLiteOS drive found by PowerShell WMI!" -ForegroundColor Green
    Write-Host "   Device ID: $($phoenixDrive.DeviceID)" -ForegroundColor Cyan
    Write-Host "   Volume Label: $($phoenixDrive.VolumeLabel)" -ForegroundColor Cyan
    Write-Host "   Drive Type: $($phoenixDrive.DriveType) (2 = Removable)" -ForegroundColor Cyan
    Write-Host "   Size: $([math]::Round($phoenixDrive.Size / 1GB, 1)) GB" -ForegroundColor Cyan
} else {
    Write-Host "? PhoenixLiteOS drive NOT found by PowerShell WMI" -ForegroundColor Red
    Write-Host "   This could indicate the drive is not ready or not properly mounted" -ForegroundColor Yellow
}

Write-Host ""

# Double-check with .NET DriveInfo
$dotNetDrives = [System.IO.DriveInfo]::GetDrives() | Where-Object { $_.IsReady -and $_.Name -eq "H:\" }

if ($dotNetDrives) {
    $drive = $dotNetDrives[0]
    Write-Host "? PhoenixLiteOS drive found by .NET DriveInfo!" -ForegroundColor Green
    Write-Host "   Name: $($drive.Name)" -ForegroundColor Cyan
    Write-Host "   Volume Label: $($drive.VolumeLabel)" -ForegroundColor Cyan
    Write-Host "   Drive Type: $($drive.DriveType)" -ForegroundColor Cyan
    Write-Host "   Size: $([math]::Round($drive.TotalSize / 1GB, 1)) GB" -ForegroundColor Cyan
    Write-Host "   Ready: $($drive.IsReady)" -ForegroundColor Cyan
} else {
    Write-Host "? PhoenixLiteOS drive NOT found by .NET DriveInfo" -ForegroundColor Red
}

Write-Host ""

Write-Host "?? EXPECTED DEBUG OUTPUT IN APP:" -ForegroundColor Yellow
Write-Host ""

Write-Host "When you launch the app, you should see:" -ForegroundColor White
Write-Host "?? === LoadDrivesAsync STARTED ===" -ForegroundColor Cyan
Write-Host "?? Most recent drive from detection service: none" -ForegroundColor Cyan
Write-Host "?? Calling GetRemovableDrivesWithHighlightAsync..." -ForegroundColor Cyan
Write-Host "?? === Starting USB-ONLY drive detection with device info ===" -ForegroundColor Cyan
Write-Host "?? Found 6 ready drives total" -ForegroundColor Cyan
Write-Host "??  Skipping system drive: C:\" -ForegroundColor Cyan
Write-Host "?? Examining drive D:\ - Type: Fixed" -ForegroundColor Cyan
Write-Host "??  Skipped drive: D:\ - Fixed (not removable)" -ForegroundColor Cyan
Write-Host "?? Examining drive E:\ - Type: Fixed" -ForegroundColor Cyan
Write-Host "??  Skipped drive: E:\ - Fixed (not removable)" -ForegroundColor Cyan
Write-Host "?? Examining drive F:\ - Type: Fixed" -ForegroundColor Cyan
Write-Host "??  Skipped drive: F:\ - Fixed (not removable)" -ForegroundColor Cyan
Write-Host "?? Examining drive G:\ - Type: Fixed" -ForegroundColor Cyan
Write-Host "??  Skipped drive: G:\ - Fixed (not removable)" -ForegroundColor Cyan
Write-Host "?? Examining drive H:\ - Type: Removable" -ForegroundColor Cyan
Write-Host "?? Creating drive object for H:\" -ForegroundColor Cyan
Write-Host "?? Getting device description for H:" -ForegroundColor Cyan
Write-Host "?? Getting device description for H" -ForegroundColor Cyan
Write-Host "?? WMI found - VolumeLabel: PhoenixLiteOS, Description: [something]" -ForegroundColor Cyan
Write-Host "?? Device description: PhoenixLiteOS USB Drive" -ForegroundColor Cyan
Write-Host "?? Checking if H: is most recent (none)..." -ForegroundColor Cyan
Write-Host "? Added USB drive: H:\ - Removable - PhoenixLiteOS USB Drive" -ForegroundColor Cyan
Write-Host "?? === Total drives returned: 1 ===" -ForegroundColor Cyan
Write-Host "   ?? H:: ?? PhoenixLiteOS (H:\) 28.6GB - PhoenixLiteOS USB Drive" -ForegroundColor Cyan
Write-Host "?? GetRemovableDrivesWithHighlightAsync returned 1 drives" -ForegroundColor Cyan
Write-Host "?? Service returned drive: H: - ?? PhoenixLiteOS (H:\) 28.6GB" -ForegroundColor Cyan
Write-Host "?? Queueing UI update..." -ForegroundColor Cyan
Write-Host "?? UI update queued successfully: True" -ForegroundColor Cyan
Write-Host "?? UpdateDriveListDirectly: Processing 1 USB drives ON UI THREAD" -ForegroundColor Cyan
Write-Host "?? Added drive: H: - ?? PhoenixLiteOS (H:\) 28.6GB" -ForegroundColor Cyan

Write-Host ""

Write-Host "?? WHAT YOU SHOULD SEE IN UI:" -ForegroundColor Yellow
Write-Host ""

Write-Host "In the USB DRIVES section:" -ForegroundColor White
Write-Host "??  ?? PhoenixLiteOS (H:\) 28.6GB" -ForegroundColor Green
Write-Host "    PhoenixLiteOS USB Drive" -ForegroundColor Gray
Write-Host "    [Usage info]" -ForegroundColor Gray
Write-Host "    [NEWEST] [USB] badges" -ForegroundColor Orange

Write-Host ""

Write-Host "?? IF YOU DON'T SEE THE DRIVE:" -ForegroundColor Red
Write-Host ""

Write-Host "1. Check if the debug output matches what's shown above" -ForegroundColor White
Write-Host "2. Look for any error messages (?) in the debug output" -ForegroundColor White
Write-Host "3. Try unplugging and re-plugging the PhoenixLiteOS drive" -ForegroundColor White
Write-Host "4. Check Windows File Explorer - can you see the H: drive there?" -ForegroundColor White
Write-Host "5. Try the Manual Refresh button in the app" -ForegroundColor White

Write-Host ""

Write-Host "?? POTENTIAL ISSUES:" -ForegroundColor Yellow
Write-Host ""

Write-Host "• WMI permission issues" -ForegroundColor White
Write-Host "• Drive not properly mounted" -ForegroundColor White
Write-Host "• UI binding problems" -ForegroundColor White
Write-Host "• Exception during drive processing" -ForegroundColor White

Write-Host ""

Write-Host "? WHAT THE FIXES DO:" -ForegroundColor Green
Write-Host "• Added comprehensive debug logging to track every step" -ForegroundColor Green
Write-Host "• Simplified device description logic" -ForegroundColor Green
Write-Host "• Added demo drive fallback if detection fails" -ForegroundColor Green
Write-Host "• Enhanced error handling with stack traces" -ForegroundColor Green

Write-Host ""
Write-Host "?? Launch the app and check if you see the PhoenixLiteOS drive!" -ForegroundColor Green