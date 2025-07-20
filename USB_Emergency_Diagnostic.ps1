# USB Drive Detection Emergency Diagnostic
# Comprehensive debugging for drives not showing in UI

Write-Host "=== USB DRIVE DETECTION EMERGENCY DIAGNOSTIC ===" -ForegroundColor Red
Write-Host ""

Write-Host "?? ISSUE: Nothing shows in the drive list" -ForegroundColor Red
Write-Host "?? SOLUTION: Comprehensive diagnosis and temporary fixes" -ForegroundColor Yellow
Write-Host ""

Write-Host "?? STEP 1: Check What Windows Sees" -ForegroundColor Yellow
Write-Host ""

# Get all drives that Windows can see
$allDrives = [System.IO.DriveInfo]::GetDrives() | Where-Object {$_.IsReady}
Write-Host "All Ready Drives Windows Detects:" -ForegroundColor Cyan
foreach ($drive in $allDrives) {
    $sizeGB = [math]::Round($drive.TotalSize / 1GB, 1)
    $label = if ([string]::IsNullOrEmpty($drive.VolumeLabel)) { "No Label" } else { $drive.VolumeLabel }
    $color = if ($drive.Name.ToUpper().StartsWith("C:")) { "Gray" } else { "White" }
    Write-Host "  $($drive.Name) - $label - $($drive.DriveType) - $sizeGB GB" -ForegroundColor $color
}

Write-Host ""

Write-Host "?? STEP 2: Filter Like The App Does" -ForegroundColor Yellow
Write-Host ""

$usbDrives = @()
$otherDrives = @()

foreach ($drive in $allDrives) {
    if ($drive.Name.ToUpper().StartsWith("C:")) { 
        Write-Host "??  Skipping system drive: $($drive.Name)" -ForegroundColor Gray
        continue 
    }
    
    Write-Host "?? Examining: $($drive.Name) - Type: $($drive.DriveType)" -ForegroundColor Cyan
    
    if ($drive.DriveType -eq "Removable") {
        $usbDrives += $drive
        Write-Host "  ? Added as USB drive (DriveType.Removable)" -ForegroundColor Green
    } else {
        $otherDrives += $drive
        Write-Host "  ? Not USB (DriveType: $($drive.DriveType))" -ForegroundColor Red
    }
}

Write-Host ""

Write-Host "?? STEP 3: Results Summary" -ForegroundColor Yellow
Write-Host ""

if ($usbDrives.Count -gt 0) {
    Write-Host "? REAL USB DRIVES FOUND: $($usbDrives.Count)" -ForegroundColor Green
    foreach ($usb in $usbDrives) {
        $sizeGB = [math]::Round($usb.TotalSize / 1GB, 1)
        $label = if ([string]::IsNullOrEmpty($usb.VolumeLabel)) { "USB Drive" } else { $usb.VolumeLabel }
        Write-Host "  ?? $label ($($usb.Name)) $sizeGB GB - USB" -ForegroundColor Green
    }
} else {
    Write-Host "? NO REAL USB DRIVES FOUND" -ForegroundColor Red
    Write-Host "The app will show demo drives instead:" -ForegroundColor Yellow
    foreach ($other in $otherDrives) {
        $sizeGB = [math]::Round($other.TotalSize / 1GB, 1)
        $label = if ([string]::IsNullOrEmpty($other.VolumeLabel)) { "Drive" } else { $other.VolumeLabel }
        Write-Host "  ?? $label ($($other.Name)) $sizeGB GB - Demo $($other.DriveType)" -ForegroundColor Cyan
    }
}

Write-Host ""

Write-Host "?? STEP 4: What To Expect in App" -ForegroundColor Yellow
Write-Host ""

if ($usbDrives.Count -gt 0) {
    Write-Host "Expected Debug Output:" -ForegroundColor White
    Write-Host "?? === Starting USB-ONLY drive detection with device info ===" -ForegroundColor Cyan
    Write-Host "?? Found $($allDrives.Count) ready drives total" -ForegroundColor Cyan
    foreach ($usb in $usbDrives) {
        Write-Host "?? Examining drive $($usb.Name) - Type: $($usb.DriveType)" -ForegroundColor Cyan
        Write-Host "? Added USB drive: $($usb.Name) - $($usb.DriveType) - Device Description" -ForegroundColor Cyan
    }
    Write-Host "?? === Total drives returned: $($usbDrives.Count) ===" -ForegroundColor Cyan
} else {
    Write-Host "Expected Debug Output (Demo Mode):" -ForegroundColor White
    Write-Host "?? === Starting USB-ONLY drive detection with device info ===" -ForegroundColor Cyan
    Write-Host "?? Found $($allDrives.Count) ready drives total" -ForegroundColor Cyan
    Write-Host "??  Skipped drive: [drives] - [types] (not removable)" -ForegroundColor Cyan
    Write-Host "?? No USB drives found - adding demo drives for debugging..." -ForegroundColor Cyan
    Write-Host "?? Demo mode: Found $($otherDrives.Count) potential demo drives" -ForegroundColor Cyan
    foreach ($other in $otherDrives) {
        Write-Host "?? Added demo drive: $($other.Name) - $($other.DriveType)" -ForegroundColor Cyan
    }
    Write-Host "?? === Total drives returned: $($otherDrives.Count) ===" -ForegroundColor Cyan
}

Write-Host ""

Write-Host "?? TROUBLESHOOTING STEPS:" -ForegroundColor Red
Write-Host ""

Write-Host "1. FIRST - Check App Debug Output:" -ForegroundColor White
Write-Host "   Look for lines starting with ??, ??, ?, ??, ??" -ForegroundColor Cyan
Write-Host "   These will tell you exactly what the app is finding" -ForegroundColor Cyan

Write-Host ""
Write-Host "2. IF NO DEBUG OUTPUT ? App crashed during initialization" -ForegroundColor White
Write-Host "   - Check for error messages in debug console" -ForegroundColor Cyan
Write-Host "   - Verify all services are starting correctly" -ForegroundColor Cyan

Write-Host ""
Write-Host "3. IF DEBUG SHOWS DRIVES BUT UI EMPTY ? UI binding issue" -ForegroundColor White
Write-Host "   - Look for 'UpdateDriveListDirectly: Processing X drives ON UI THREAD'" -ForegroundColor Cyan
Write-Host "   - Check for 'Added drive: X - [name]' messages" -ForegroundColor Cyan

Write-Host ""
Write-Host "4. IF DEBUG SHOWS NO DRIVES ? Detection issue" -ForegroundColor White
Write-Host "   - The script above should show demo drives at minimum" -ForegroundColor Cyan
Write-Host "   - Try plugging in a USB drive and check if it shows as 'Removable'" -ForegroundColor Cyan

Write-Host ""

Write-Host "? IMMEDIATE ACTIONS:" -ForegroundColor Yellow
Write-Host ""

Write-Host "1. Launch the app and check the debug output" -ForegroundColor White
Write-Host "2. Look for the detection messages listed above" -ForegroundColor White
Write-Host "3. Try the Manual Refresh button" -ForegroundColor White
Write-Host "4. Try plugging/unplugging a USB drive" -ForegroundColor White
Write-Host "5. Report back what debug messages you see (or don't see)" -ForegroundColor White

Write-Host ""

Write-Host "?? TEMPORARY FIX APPLIED:" -ForegroundColor Green
Write-Host "? App will now show demo drives if no USB drives found" -ForegroundColor Green
Write-Host "? More detailed debug logging added" -ForegroundColor Green
Write-Host "? You should see SOMETHING in the drive list now" -ForegroundColor Green

Write-Host ""
Write-Host "?? Ready for detailed diagnosis!" -ForegroundColor Green