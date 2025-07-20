# USB Detection Debug Helper
# This script helps debug why USB drives are detected but not showing in UI

Write-Host "=== USB DETECTION DEBUG HELPER ===" -ForegroundColor Green
Write-Host ""

Write-Host "?? Current Drive Status:" -ForegroundColor Yellow
$allDrives = [System.IO.DriveInfo]::GetDrives() | Where-Object {$_.IsReady}

foreach ($drive in $allDrives) {
    if ($drive.Name.ToUpper().StartsWith("C:")) { continue }
    
    $sizeGB = [math]::Round($drive.TotalSize / 1GB, 1)
    $letter = $drive.Name[0]
    $label = if ([string]::IsNullOrEmpty($drive.VolumeLabel)) { "No Label" } else { $drive.VolumeLabel }
    
    $shouldDetect = $false
    if ($drive.DriveType -eq "Removable") {
        $shouldDetect = $true
        $reason = "? DriveType.Removable"
    } elseif ($drive.DriveType -eq "Fixed" -and $sizeGB -lt 2000 -and $letter -ge 'D') {
        $shouldDetect = $true
        $reason = "? Small Fixed Drive (Likely USB)"
    } else {
        $reason = "? Not USB criteria"
    }
    
    $color = if ($shouldDetect) { "Green" } else { "Gray" }
    Write-Host "  $($drive.Name) - $label - $($drive.DriveType) - $sizeGB GB - $reason" -ForegroundColor $color
}

Write-Host ""

Write-Host "?? Testing Expected Behavior:" -ForegroundColor Yellow
Write-Host "1. Launch the GameDeploy app" -ForegroundColor White
Write-Host "2. Plug in your USB drive (PhoenixLiteOS H:)" -ForegroundColor White
Write-Host "3. Watch the Debug Output for these messages:" -ForegroundColor White
Write-Host "   ?? USB drives ADDED: H" -ForegroundColor Cyan
Write-Host "   ?? USB drive change event #X received - FORCING UI UPDATE" -ForegroundColor Cyan
Write-Host "   ?? FORCE UI UPDATE: Got X drives from service" -ForegroundColor Cyan
Write-Host "   ? UI updated via DispatcherQueue.GetForCurrentThread()" -ForegroundColor Cyan
Write-Host "   ?? Added drive: H - ?? PhoenixLiteOS (H:\) 28.6GB - USB/Removable" -ForegroundColor Cyan

Write-Host ""

Write-Host "4. In the UI, you should see:" -ForegroundColor White
Write-Host "   - Status message: '?? USB drive change detected (#X) - list updated!'" -ForegroundColor Cyan
Write-Host "   - Drive appears in USB DRIVES list with ?? icon" -ForegroundColor Cyan
Write-Host "   - Drive shows as 'PhoenixLiteOS (H:\) 28.6GB - USB/Removable'" -ForegroundColor Cyan

Write-Host ""

Write-Host "5. If the drive is detected but NOT showing in UI:" -ForegroundColor Yellow
Write-Host "   - Check for 'FORCE UI UPDATE' messages in debug output" -ForegroundColor White
Write-Host "   - Look for any error messages in the debug log" -ForegroundColor White
Write-Host "   - Try clicking 'Manual Refresh' button to force update" -ForegroundColor White
Write-Host "   - Try the Settings button (??) to force refresh everything" -ForegroundColor White

Write-Host ""

Write-Host "?? What the NEW fixes do:" -ForegroundColor Green
Write-Host "? Improved USB event handling with multiple fallback methods" -ForegroundColor Green
Write-Host "? Force UI update when USB events are detected" -ForegroundColor Green
Write-Host "? Comprehensive debug logging to track exactly what happens" -ForegroundColor Green
Write-Host "? Fallback methods if DispatcherQueue fails" -ForegroundColor Green
Write-Host "? Better status messages showing USB event counts" -ForegroundColor Green

Write-Host ""

Write-Host "?? Key Debug Messages to Look For:" -ForegroundColor Yellow
Write-Host "USB Detection:" -ForegroundColor White
Write-Host "  '?? USB drives ADDED: H' - USB service detected the drive" -ForegroundColor Cyan
Write-Host "  '?? USB drive change event #X received' - Event fired to UI" -ForegroundColor Cyan

Write-Host ""
Write-Host "UI Update:" -ForegroundColor White
Write-Host "  '?? FORCE UI UPDATE: Starting drive refresh' - UI update began" -ForegroundColor Cyan
Write-Host "  '?? FORCE UI UPDATE: Got X drives from service' - Drive data retrieved" -ForegroundColor Cyan
Write-Host "  '? UI updated via DispatcherQueue' - UI successfully updated" -ForegroundColor Cyan

Write-Host ""
Write-Host "Drive Processing:" -ForegroundColor White
Write-Host "  '?? Added drive: H - ?? PhoenixLiteOS...' - Drive added to UI list" -ForegroundColor Cyan
Write-Host "  '? UpdateDriveList: Complete - X drives now in UI' - Process finished" -ForegroundColor Cyan

Write-Host ""
Write-Host "?? If you still don't see the drive in UI after these fixes:" -ForegroundColor Red
Write-Host "1. Check the debug output for the exact messages above" -ForegroundColor White
Write-Host "2. Look for any error messages (?)" -ForegroundColor White
Write-Host "3. Try unplugging and re-plugging the USB drive" -ForegroundColor White
Write-Host "4. Use the 'Manual Refresh' button as a test" -ForegroundColor White

Write-Host ""
Write-Host "Ready to test! The detection should now work with force UI updates." -ForegroundColor Green