# Simple USB Detection Test
# Tests the new simple and efficient detection method

Write-Host "=== SIMPLE USB DETECTION TEST ===" -ForegroundColor Green
Write-Host ""

Write-Host "?? Testing the NEW simple detection method..." -ForegroundColor Yellow
Write-Host ""

# Method 1: Check what .NET DriveInfo sees
Write-Host "1. All Ready Drives (what .NET sees):" -ForegroundColor Cyan
$allDrives = [System.IO.DriveInfo]::GetDrives() | Where-Object {$_.IsReady}

foreach ($drive in $allDrives) {
    $sizeGB = [math]::Round($drive.TotalSize / 1GB, 1)
    $letter = $drive.Name[0]
    $label = if ([string]::IsNullOrEmpty($drive.VolumeLabel)) { "No Label" } else { $drive.VolumeLabel }
    
    # Apply the new simple logic
    $shouldInclude = $false
    $reason = ""
    
    if ($drive.Name.ToUpper().StartsWith("C:")) {
        $reason = "? System Drive (Excluded)"
    }
    elseif ($drive.DriveType -eq "Removable") {
        $shouldInclude = $true
        $reason = "? DriveType.Removable (Always USB)"
    }
    elseif ($drive.DriveType -eq "Fixed" -and $sizeGB -lt 2000 -and $letter -ge 'D') {
        $shouldInclude = $true
        $reason = "? Small Fixed Drive (Likely USB)"
    }
    else {
        $reason = "? Large Fixed Drive (Likely Internal)"
    }
    
    $color = if ($shouldInclude) { "Green" } else { "Gray" }
    Write-Host "  $($drive.Name) - $label - $($drive.DriveType) - $sizeGB GB - $reason" -ForegroundColor $color
}

Write-Host ""

# Method 2: Simulate the new detection logic
Write-Host "2. NEW Detection Logic Results:" -ForegroundColor Yellow
$usbDrives = @()
$demoDrives = @()

foreach ($drive in $allDrives) {
    if ($drive.Name.ToUpper().StartsWith("C:")) { continue }
    
    $sizeGB = [math]::Round($drive.TotalSize / 1GB, 1)
    $letter = $drive.Name[0]
    
    $isRemovable = $false
    if ($drive.DriveType -eq "Removable") {
        $isRemovable = $true
    }
    elseif ($drive.DriveType -eq "Fixed" -and $sizeGB -lt 2000 -and $letter -ge 'D') {
        $isRemovable = $true
    }
    
    if ($isRemovable) {
        $usbDrives += $drive
    } else {
        $demoDrives += $drive
    }
}

if ($usbDrives.Count -gt 0) {
    Write-Host "? USB/Removable Drives Found:" -ForegroundColor Green
    foreach ($drive in $usbDrives) {
        $sizeGB = [math]::Round($drive.TotalSize / 1GB, 1)
        $label = if ([string]::IsNullOrEmpty($drive.VolumeLabel)) { "Drive" } else { $drive.VolumeLabel }
        Write-Host "   ?? $label ($($drive.Name)) $sizeGB GB - USB/Removable" -ForegroundColor Green
    }
} else {
    Write-Host "??  No USB drives found, would show demo drives:" -ForegroundColor Yellow
    $demoCount = [math]::Min(3, $demoDrives.Count)
    for ($i = 0; $i -lt $demoCount; $i++) {
        $drive = $demoDrives[$i]
        $sizeGB = [math]::Round($drive.TotalSize / 1GB, 1)
        $label = if ([string]::IsNullOrEmpty($drive.VolumeLabel)) { "Drive" } else { $drive.VolumeLabel }
        Write-Host "   ?? $label ($($drive.Name)) $sizeGB GB - Demo Mode" -ForegroundColor Gray
    }
}

Write-Host ""

# Method 3: Real-time change detection simulation
Write-Host "3. Testing Change Detection:" -ForegroundColor Yellow
$currentDrives = @()
foreach ($drive in $allDrives) {
    if ($drive.Name.ToUpper().StartsWith("C:")) { continue }
    
    $sizeGB = [math]::Round($drive.TotalSize / 1GB, 1)
    $letter = $drive.Name[0]
    
    if ($drive.DriveType -eq "Removable" -or 
        ($drive.DriveType -eq "Fixed" -and $sizeGB -lt 2000 -and $letter -ge 'D')) {
        $currentDrives += $drive.Name.TrimEnd('\')
    }
}

Write-Host "Current USB drives that would be monitored: $($currentDrives -join ', ')" -ForegroundColor Cyan
Write-Host "The app will detect changes by comparing this list every 1 second" -ForegroundColor White

Write-Host ""

# Instructions
Write-Host "4. How the NEW system works:" -ForegroundColor Yellow
Write-Host "? SIMPLE: No complex WMI queries or event watching" -ForegroundColor Green
Write-Host "? RELIABLE: Uses basic .NET DriveInfo every 1 second" -ForegroundColor Green
Write-Host "? FAST: Detects changes within 1 second maximum" -ForegroundColor Green
Write-Host "? CLEAR: Shows exactly what logic is applied to each drive" -ForegroundColor Green

Write-Host ""
Write-Host "5. Testing Instructions:" -ForegroundColor Yellow
Write-Host "1. Run the GameDeploy app" -ForegroundColor White
Write-Host "2. Plug in a USB drive" -ForegroundColor White
Write-Host "3. It should appear within 1 second" -ForegroundColor White
Write-Host "4. Unplug the USB drive" -ForegroundColor White
Write-Host "5. It should disappear within 1 second" -ForegroundColor White
Write-Host "6. Check the debug output for detailed logging" -ForegroundColor White

Write-Host ""
Write-Host "?? This new approach is MUCH simpler and more reliable!" -ForegroundColor Green