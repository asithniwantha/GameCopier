# Drive Detection Diagnostic Script
# Run this to see exactly what drives are available and their properties

Write-Host "=== Drive Detection Diagnostic ===" -ForegroundColor Green
Write-Host ""

# Method 1: .NET DriveInfo approach (what the app uses)
Write-Host "1. .NET DriveInfo Detection:" -ForegroundColor Yellow
$allDrives = [System.IO.DriveInfo]::GetDrives() | Where-Object {$_.IsReady}
foreach ($drive in $allDrives) {
    $sizeGB = [math]::Round($drive.TotalSize / 1GB, 1)
    $freeGB = [math]::Round($drive.AvailableFreeSpace / 1GB, 1)
    $label = if ([string]::IsNullOrEmpty($drive.VolumeLabel)) { "No Label" } else { $drive.VolumeLabel }
    
    $status = switch ($drive.Name.ToUpper()) {
        "C:\" { "? System Drive (Excluded)" }
        default { 
            switch ($drive.DriveType) {
                "Removable" { "? Removable Drive (Included)" }
                "Fixed" { "?? Fixed Drive (USB Check Required)" }
                default { "?? Other Type (May be Excluded)" }
            }
        }
    }
    
    Write-Host "  $($drive.Name) - $label - $($drive.DriveType) - $sizeGB GB - $status" -ForegroundColor Cyan
}

Write-Host ""

# Method 2: WMI approach for USB detection
Write-Host "2. WMI USB Drive Detection:" -ForegroundColor Yellow
try {
    $usbDrives = Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DriveType -eq 2}
    if ($usbDrives) {
        foreach ($drive in $usbDrives) {
            $sizeGB = [math]::Round($drive.Size / 1GB, 1)
            $label = if ([string]::IsNullOrEmpty($drive.VolumeName)) { "No Label" } else { $drive.VolumeName }
            Write-Host "  $($drive.DeviceID) - $label - Removable - $sizeGB GB - ? True USB Drive" -ForegroundColor Green
        }
    } else {
        Write-Host "  No USB drives detected via WMI DriveType=2" -ForegroundColor Red
    }
} catch {
    Write-Host "  WMI query failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Method 3: Advanced USB detection
Write-Host "3. Advanced USB Interface Detection:" -ForegroundColor Yellow
try {
    $query = @"
    SELECT ld.DeviceID, ld.VolumeName, ld.Size, dd.InterfaceType, dd.Caption, dd.Model
    FROM Win32_LogicalDisk ld
    INNER JOIN Win32_LogicalDiskToPartition ldp ON ld.DeviceID = ldp.Dependent
    INNER JOIN Win32_DiskPartition dp ON ldp.Antecedent = dp.DeviceID
    INNER JOIN Win32_DiskDriveToDiskPartition ddp ON dp.DeviceID = ddp.Dependent
    INNER JOIN Win32_DiskDrive dd ON ddp.Antecedent = dd.DeviceID
    WHERE ld.DriveType = 3 AND ld.DeviceID != 'C:'
"@

    $results = Get-WmiObject -Query $query
    foreach ($result in $results) {
        $sizeGB = [math]::Round($result.Size / 1GB, 1)
        $label = if ([string]::IsNullOrEmpty($result.VolumeName)) { "No Label" } else { $result.VolumeName }
        $isUsb = $result.InterfaceType -eq "USB" -or $result.Caption -like "*USB*" -or $result.Model -like "*USB*"
        $status = if ($isUsb) { "? USB Interface Detected" } else { "? Not USB ($($result.InterfaceType))" }
        
        Write-Host "  $($result.DeviceID) - $label - $sizeGB GB - $status" -ForegroundColor $(if ($isUsb) { "Green" } else { "Gray" })
    }
} catch {
    Write-Host "  Advanced WMI query failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# What the app should show
Write-Host "4. Expected App Behavior:" -ForegroundColor Yellow
Write-Host "  The app will show drives in this order of preference:" -ForegroundColor White
Write-Host "  1. DriveType=Removable drives (? Always included)" -ForegroundColor Green
Write-Host "  2. DriveType=Fixed drives that are USB (? Included if USB detected)" -ForegroundColor Green
Write-Host "  3. Non-C: drives as demo mode (?? Only if no USB found)" -ForegroundColor Yellow
Write-Host "  4. Each drive gets ?? icon if removable/USB, ?? if demo" -ForegroundColor White

Write-Host ""

# Testing instructions
Write-Host "5. Testing Instructions:" -ForegroundColor Yellow
Write-Host "  1. Plug in a USB drive" -ForegroundColor White
Write-Host "  2. Run this script again to see if it's detected" -ForegroundColor White
Write-Host "  3. Launch the GameDeploy app and check the USB DRIVES section" -ForegroundColor White
Write-Host "  4. Look for drives with ?? icons (real USB) vs ?? icons (demo)" -ForegroundColor White
Write-Host "  5. Check the Output/Debug window for detailed detection logs" -ForegroundColor White

Write-Host ""
Write-Host "Diagnostic complete! If USB drives still don't appear, check the app's debug output." -ForegroundColor Green