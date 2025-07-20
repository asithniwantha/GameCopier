# USB Detection Test Script
# This script helps verify that USB detection events are working

Write-Host "=== USB Detection Test ===" -ForegroundColor Green
Write-Host ""

# Test 1: Check current drives
Write-Host "Current drives detected:" -ForegroundColor Yellow
$drives = Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DriveType -eq 2 -or ($_.DriveType -eq 3 -and $_.DeviceID -ne "C:")}
foreach ($drive in $drives) {
    $type = switch ($drive.DriveType) {
        2 { "Removable" }
        3 { "Fixed" }
        default { "Other" }
    }
    $size = [math]::Round($drive.Size / 1GB, 2)
    $free = [math]::Round($drive.FreeSpace / 1GB, 2)
    Write-Host "  $($drive.DeviceID) - $($drive.VolumeName) ($type) - $size GB total, $free GB free" -ForegroundColor Cyan
}

Write-Host ""

# Test 2: Check WMI event capability
Write-Host "Testing WMI event monitoring capability..." -ForegroundColor Yellow
try {
    $query = "SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2"
    $watcher = New-Object System.Management.ManagementEventWatcher($query)
    Write-Host "? WMI event monitoring is supported" -ForegroundColor Green
    $watcher.Dispose()
} catch {
    Write-Host "? WMI event monitoring failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Instructions for manual testing
Write-Host "Manual Testing Instructions:" -ForegroundColor Yellow
Write-Host "1. Launch the GameDeploy Kiosk application" -ForegroundColor White
Write-Host "2. Look for the green 'USB LIVE' indicator in the header" -ForegroundColor White
Write-Host "3. Insert a USB drive and watch for:" -ForegroundColor White
Write-Host "   - Status message showing 'USB drive change detected'" -ForegroundColor Cyan
Write-Host "   - Drive appears in the USB DRIVES list with ?? icon" -ForegroundColor Cyan
Write-Host "   - USB event counter increments in status" -ForegroundColor Cyan
Write-Host "4. Remove the USB drive and verify it disappears" -ForegroundColor White
Write-Host "5. Check the Output/Debug window for detection logs" -ForegroundColor White

Write-Host ""

# Test 4: Run live monitoring (optional)
$runLiveTest = Read-Host "Run live USB monitoring test? (y/n)"
if ($runLiveTest -eq 'y' -or $runLiveTest -eq 'Y') {
    Write-Host ""
    Write-Host "Starting live USB monitoring... Press Ctrl+C to stop" -ForegroundColor Green
    Write-Host "Plug or unplug USB drives to see events" -ForegroundColor Yellow
    
    try {
        Register-WmiEvent -Query "SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2" -Action {
            $event = $Event.SourceEventArgs.NewEvent
            Write-Host "?? USB INSERTED: $($event.DriveName)" -ForegroundColor Green
        } -SourceIdentifier "USBInsert"
        
        Register-WmiEvent -Query "SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 3" -Action {
            $event = $Event.SourceEventArgs.NewEvent
            Write-Host "?? USB REMOVED: $($event.DriveName)" -ForegroundColor Red
        } -SourceIdentifier "USBRemove"
        
        # Keep monitoring
        while ($true) {
            Start-Sleep -Seconds 1
        }
    } finally {
        Unregister-Event -SourceIdentifier "USBInsert" -ErrorAction SilentlyContinue
        Unregister-Event -SourceIdentifier "USBRemove" -ErrorAction SilentlyContinue
        Write-Host "Live monitoring stopped" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Test completed! Launch the GameDeploy application to verify real-time detection." -ForegroundColor Green