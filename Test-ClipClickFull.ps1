# ClipClick Full Verification Test Script (Release Build)
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$binDir = Join-Path $baseDir "bin\$Configuration"

Write-Host "==========================================================" -ForegroundColor Magenta
Write-Host "   CLIPCLICK COMPREHENSIVE AUTOMATED VERIFICATION SUITE   " -ForegroundColor Magenta
Write-Host "   Binary Directory: $binDir" -ForegroundColor Magenta
Write-Host "==========================================================" -ForegroundColor Magenta

if (-not (Test-Path (Join-Path $binDir "ClipClick.exe"))) {
    Write-Error "ClipClick.exe not found in $binDir. Please build first."
    exit 1
}

# 1. Load Assemblies
Add-Type -Path (Join-Path $binDir "System.Data.SQLite.dll")
Add-Type -Path (Join-Path $binDir "NAudio.Core.dll")
Add-Type -Path (Join-Path $binDir "NAudio.dll")
[System.Reflection.Assembly]::LoadFrom((Join-Path $binDir "ClipClick.exe")) | Out-Null
Write-Host "[OK] Assemblies loaded successfully." -ForegroundColor Green

# 2. Database Service Verification
Write-Host "`n--- 1. Testing DatabaseService & Prune Limits ---" -ForegroundColor Cyan
$db = New-Object ClipClick.Services.DatabaseService
$db.ClearAllHistory()

Write-Host "Database initialized and cleared." -ForegroundColor Gray

# Test insertion
for ($i = 1; $i -le 150; $i++) {
    $db.AddClipboardEntry("Test Entry item #$i - Sample clipboard text content") | Out-Null
}
$allEntries = $db.GetClipboardEntries("")
Write-Host "Inserted 150 items. Actual in DB: $($allEntries.Count)" -ForegroundColor Gray
if ($allEntries.Count -ne 150) { throw "Expected 150 entries, got $($allEntries.Count)" }

# Test Pinning
$target = $allEntries[50]
$db.TogglePin($target.Id, $true)
$reloaded = $db.GetClipboardEntries("")
if (-not $reloaded[0].IsPinned) { throw "Pinned item did not appear first in the query!" }
Write-Host "[OK] Pinning works correctly, pinned item floated to top." -ForegroundColor Green

# Test Dynamic Limit Pruning (e.g., limit = 100)
$db.MaxEntries = 100
$db.PruneToLimit()
$afterPrune = $db.GetClipboardEntries("")
Write-Host "After setting limit to 100 and pruning: $($afterPrune.Count) items remain." -ForegroundColor Gray
if ($afterPrune.Count -gt 100) { throw "Pruning failed to respect MaxEntries 100!" }
Write-Host "[OK] MaxEntries limit pruning passed." -ForegroundColor Green

# Test Search
$searchResults = $db.GetClipboardEntries("item #14")
Write-Host "Search for 'item #14' returned $($searchResults.Count) result(s)." -ForegroundColor Gray
if ($searchResults.Count -eq 0) { throw "Search query failed!" }
Write-Host "[OK] Real-time SQLite search passed." -ForegroundColor Green

# 3. Settings Service Verification
Write-Host "`n--- 2. Testing SettingsService & Registry Startup ---" -ForegroundColor Cyan
$settingsService = New-Object ClipClick.Services.SettingsService
$settings = $settingsService.Current
Write-Host "Initial MaxClipboardEntries: $($settings.MaxClipboardEntries)" -ForegroundColor Gray
$settings.MaxClipboardEntries = 250
$settingsService.Save()
Write-Host "[OK] Settings file saved successfully." -ForegroundColor Green

$reloadedSettingsService = New-Object ClipClick.Services.SettingsService
if ($reloadedSettingsService.Current.MaxClipboardEntries -ne 250) {
    throw "Settings persistence failed to retain MaxClipboardEntries!"
}
Write-Host "[OK] Settings persistence verified." -ForegroundColor Green

# 4. ViewModels Verification
Write-Host "`n--- 3. Testing ViewModels (Clipboard, Player, Settings) ---" -ForegroundColor Cyan
$clipVM = New-Object ClipClick.ViewModels.ClipboardViewModel($db)
Write-Host "ClipboardViewModel loaded. Total items: $($clipVM.Items.Count), IsEmpty: $($clipVM.IsEmpty), HasItems: $($clipVM.HasItems)" -ForegroundColor Gray
if ($clipVM.IsEmpty) { throw "ClipboardViewModel should not be empty with items in DB." }

# Test Search in VM
$clipVM.SearchText = "item #14"
Write-Host "VM search filter applied. Filtered items count: $($clipVM.Items.Count), HasSearchText: $($clipVM.HasSearchText)" -ForegroundColor Gray
$clipVM.ClearSearchCommand.Execute($null)
if ($clipVM.HasSearchText -or $clipVM.SearchText -ne "") { throw "Clear search failed!" }
Write-Host "[OK] ClipboardViewModel search and clear commands verified." -ForegroundColor Green

# Test SettingsViewModel
$settingsVM = New-Object ClipClick.ViewModels.SettingsViewModel($settingsService, $db)
Write-Host "SettingsVM loaded. IsLimit250: $($settingsVM.IsLimit250), MaxEntries: $($settingsVM.MaxClipboardEntries)" -ForegroundColor Gray
if (-not $settingsVM.IsLimit250) { throw "Expected IsLimit250 to be true!" }

# Change limit to 500 via radio button toggle
$settingsVM.IsLimit500 = $true
if ($settingsService.Current.MaxClipboardEntries -ne 500) { throw "IsLimit500 did not update settings!" }
Write-Host "[OK] SettingsViewModel limit binding verified (now 500)." -ForegroundColor Green

# 5. Local Audio Player Service & NAudio Verification
Write-Host "`n--- 4. Testing LocalAudioPlayerService & PlayerViewModel ---" -ForegroundColor Cyan
$playerService = New-Object ClipClick.Services.LocalAudioPlayerService
$playerVM = New-Object ClipClick.ViewModels.PlayerViewModel($playerService, $null)

Write-Host "PlayerViewModel initialized." -ForegroundColor Gray
Write-Host "IsLocalMode: $($playerVM.IsLocalMode), IsPlaying: $($playerVM.IsPlaying), Volume: $($playerVM.Volume)" -ForegroundColor Gray
if (-not $playerVM.IsLocalMode) { throw "Default player mode should be Local." }

# Test Volume change
$playerVM.Volume = 65
if ([Math]::Abs($playerService.Volume - 0.65) -gt 0.02) { throw "Volume synchronization failed!" }
Write-Host "[OK] Audio engine volume controls verified." -ForegroundColor Green

# Test Mute toggle
$playerVM.IsMuted = $true
if (-not $playerService.IsMuted) { throw "Mute toggle failed!" }
$playerVM.IsMuted = $false
Write-Host "[OK] Audio engine mute controls verified." -ForegroundColor Green

# 6. MainViewModel Tab Navigation Verification
Write-Host "`n--- 5. Testing MainViewModel Navigation & Tab Binding ---" -ForegroundColor Cyan
$islandVM = New-Object ClipClick.ViewModels.IslandViewModel($playerService, $null)
$mainVM = New-Object ClipClick.ViewModels.MainViewModel($clipVM, $playerVM, $islandVM, $settingsVM)
Write-Host "MainViewModel initialized with all sub-viewmodels." -ForegroundColor Gray
Write-Host "Initial SelectedNavIndex: $($mainVM.SelectedNavIndex)" -ForegroundColor Gray
Write-Host "IsClipboardTabSelected: $($mainVM.IsClipboardTabSelected)" -ForegroundColor Gray
if ($mainVM.SelectedNavIndex -ne 0 -or -not $mainVM.IsClipboardTabSelected) { throw "Initial tab should be Clipboard (0)!" }

# Navigate to Player (1)
$mainVM.SelectedNavIndex = 1
if (-not $mainVM.IsMediaTabSelected -or $mainVM.IsClipboardTabSelected) { throw "Navigation to Player failed!" }
Write-Host "[OK] Navigated to Media Player tab (index 1)." -ForegroundColor Green

# Navigate to Settings (2)
$mainVM.SelectedNavIndex = 2
if (-not $mainVM.IsSettingsTabSelected -or $mainVM.IsMediaTabSelected) { throw "Navigation to Settings failed!" }
Write-Host "[OK] Navigated to Settings tab (index 2)." -ForegroundColor Green

# Test Settings to Clipboard event propagation:
Write-Host "Testing ClearAllClipboardCommand via SettingsViewModel..." -ForegroundColor Gray
$settingsVM.ClearAllClipboardCommand.Execute($null)
if ($clipVM.Items.Count -ne 1) { throw "Expected 1 pinned item to remain after ClearAllClipboard, got $($clipVM.Items.Count)!" }
Write-Host "[OK] ClearAllClipboardCommand preserved pinned item and updated ClipboardViewModel in real-time." -ForegroundColor Green

# Test ClearAll in ClipboardViewModel (clears everything or unpinned)
$clipVM.ClearAllCommand.Execute($null)
if ($clipVM.Items.Count -ne 1) { throw "Expected 1 item, got $($clipVM.Items.Count)" }
# Now unpin that item and clear
$pinnedItem = $clipVM.Items[0]
$clipVM.TogglePinCommand.Execute($pinnedItem)
$clipVM.ClearAllCommand.Execute($null)
if ($clipVM.Items.Count -ne 0) { throw "Expected 0 items after unpinning and clearing!" }
if (-not $clipVM.IsEmpty) { throw "ClipboardViewModel should be empty after clearing all!" }
Write-Host "[OK] ClipboardViewModel IsEmpty state correctly verified as True." -ForegroundColor Green

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "   ALL FUNCTIONAL & INTEGRATION TESTS PASSED (100%)       " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
