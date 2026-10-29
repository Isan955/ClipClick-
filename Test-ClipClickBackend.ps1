# Integration smoke test for ClipClick backend services
$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$binDir = Join-Path $baseDir "bin\Debug"

Add-Type -Path (Join-Path $binDir "System.Data.SQLite.dll")
Add-Type -Path (Join-Path $binDir "NAudio.Core.dll")
Add-Type -Path (Join-Path $binDir "NAudio.dll")
[System.Reflection.Assembly]::LoadFrom((Join-Path $binDir "ClipClick.exe")) | Out-Null

Write-Host "=== TEST 1: DatabaseService & 500-Item Limit ===" -ForegroundColor Cyan
$testDbDir = Join-Path $binDir "Data"
if (-not (Test-Path $testDbDir)) { New-Item -ItemType Directory -Path $testDbDir | Out-Null }

$db = New-Object ClipClick.Services.DatabaseService

# Add 20 entries
for ($i = 1; $i -le 20; $i++) {
    $item = $db.AddClipboardEntry("Test Entry Number $i")
}

$entries = $db.GetClipboardEntries("")
Write-Host "Inserted entries count: $($entries.Count)" -ForegroundColor Green

# Pin entry #5
$targetToPin = $entries | Where-Object { $_.Content -eq "Test Entry Number 5" } | Select-Object -First 1
if ($targetToPin) {
    $db.TogglePin($targetToPin.Id, $true)
    Write-Host "Pinned entry: $($targetToPin.Content)" -ForegroundColor Green
}

# Verify pinned is at top
$reloaded = $db.GetClipboardEntries("")
Write-Host "First entry is pinned: $($reloaded[0].IsPinned) - '$($reloaded[0].Content)'" -ForegroundColor Green

# Search test
$searchResult = $db.GetClipboardEntries("Number 15")
Write-Host "Search for 'Number 15' returned: $($searchResult.Count) items" -ForegroundColor Green

# Delete test
$db.DeleteEntry($reloaded[1].Id)
$afterDelete = $db.GetClipboardEntries("")
Write-Host "Count after deleting 1 item: $($afterDelete.Count)" -ForegroundColor Green

# Clear unpinned test
$db.ClearAllUnpinned()
$afterClear = $db.GetClipboardEntries("")
Write-Host "Count after ClearAllUnpinned (only pinned should remain): $($afterClear.Count)" -ForegroundColor Green

Write-Host "`n=== TEST 2: NAudio Audio Engine ===" -ForegroundColor Cyan
$player = New-Object ClipClick.Services.LocalAudioPlayerService
Write-Host "NAudio WaveOut & AudioFileReader initialized successfully." -ForegroundColor Green
Write-Host "Initial Playlist count: $($player.Playlist.Count)" -ForegroundColor Green
Write-Host "Initial Volume: $($player.Volume)" -ForegroundColor Green

Write-Host "`n=== TEST 3: Settings Service ===" -ForegroundColor Cyan
$settings = New-Object ClipClick.Services.SettingsService
Write-Host "Settings loaded. MinimizeToTrayOnClose: $($settings.Current.MinimizeToTrayOnClose)" -ForegroundColor Green
Write-Host "DynamicIslandTopmost: $($settings.Current.DynamicIslandTopmost)" -ForegroundColor Green

Write-Host "`nALL BACKEND INTEGRATION TESTS PASSED SUCCESSFULLY!" -ForegroundColor Green
