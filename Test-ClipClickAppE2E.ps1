# End-to-End Live App Test
param(
    [string]$Configuration = "Release"
)

$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$exePath = Join-Path $baseDir "bin\$Configuration\ClipClick.exe"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   CLIPCLICK LIVE E2E APPLICATION INTEGRATION TEST        " -ForegroundColor Cyan
Write-Host "   Testing: $exePath" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

if (-not (Test-Path $exePath)) {
    Write-Error "ClipClick.exe does not exist at $exePath"
    exit 1
}

# Add Windows Forms for resilient clipboard operations in test
Add-Type -AssemblyName System.Windows.Forms

function Set-SafeClipboardText([string]$text) {
    for ($attempt = 1; $attempt -le 10; $attempt++) {
        try {
            [System.Windows.Forms.Clipboard]::SetText($text)
            return
        }
        catch {
            Start-Sleep -Milliseconds 60
        }
    }
    throw "Failed to set clipboard text after 10 retries"
}

# Kill any existing ClipClick processes
Get-Process ClipClick -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Write-Host "Starting ClipClick application..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $exePath -PassThru

try {
    # Give UI and clipboard listener 2.5 seconds to initialize
    Start-Sleep -Milliseconds 2500

    if ($proc.HasExited) {
        throw "ClipClick exited prematurely with code $($proc.ExitCode). Check crash.log if available."
    }
    Write-Host "[OK] ClipClick process is running (PID: $($proc.Id))." -ForegroundColor Green

    # Prepare DB accessor for checking contents
    $binDir = Join-Path $baseDir "bin\$Configuration"
    Add-Type -Path (Join-Path $binDir "System.Data.SQLite.dll") -ErrorAction SilentlyContinue
    [System.Reflection.Assembly]::LoadFrom($exePath) | Out-Null
    $db = New-Object ClipClick.Services.DatabaseService

    # Test 1: Single line text capture
    $testContent1 = "ClipClick-AutoCapture-1-" + (Get-Random -Minimum 10000 -Maximum 99999)
    Write-Host "Simulating user copy 1: '$testContent1'..." -ForegroundColor Yellow
    Set-SafeClipboardText $testContent1
    Start-Sleep -Milliseconds 1500

    $entries1 = $db.GetClipboardEntries($testContent1)
    if ($entries1.Count -gt 0 -and $entries1[0].Content -eq $testContent1) {
        Write-Host "[SUCCESS] Copy 1 captured in real-time by ClipClick (ID: $($entries1[0].Id))!" -ForegroundColor Green
    } else {
        throw "ClipClick did not capture copy 1 into database!"
    }

    # Test 2: Multiline text capture
    $testContent2 = "Line 1: ClipClick Neon Green`r`nLine 2: Target #B6FF00 & Modern UI"
    Write-Host "Simulating user copy 2 (multiline)..." -ForegroundColor Yellow
    Set-SafeClipboardText $testContent2
    Start-Sleep -Milliseconds 1500

    $entries2 = $db.GetClipboardEntries("Line 1: ClipClick Neon Green")
    if ($entries2.Count -gt 0) {
        Write-Host "[SUCCESS] Copy 2 (multiline) captured in real-time by ClipClick (ID: $($entries2[0].Id))!" -ForegroundColor Green
    } else {
        throw "ClipClick did not capture copy 2 into database!"
    }

    # Test 3: Third copy with symbols and numbers
    $testContent3 = "https://github.com/clipclick/releases/tag/v1.0.0?ref=main&build=2026"
    Write-Host "Simulating user copy 3 (URL / symbols)..." -ForegroundColor Yellow
    Set-SafeClipboardText $testContent3
    Start-Sleep -Milliseconds 1500

    $entries3 = $db.GetClipboardEntries($testContent3)
    if ($entries3.Count -gt 0) {
        Write-Host "[SUCCESS] Copy 3 (URL) captured in real-time by ClipClick (ID: $($entries3[0].Id))!" -ForegroundColor Green
    } else {
        throw "ClipClick did not capture copy 3 into database!"
    }

    Write-Host "`n[OK] ALL REAL-TIME CLIPBOARD AUTO-CAPTURE TESTS PASSED WITH 100% SUCCESS!" -ForegroundColor Green
}
finally {
    if ($proc -and -not $proc.HasExited) {
        Write-Host "Stopping test ClipClick instance..." -ForegroundColor Gray
        $proc.Kill()
        $proc.WaitForExit(3000)
    }
}
