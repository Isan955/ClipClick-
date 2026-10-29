$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$exePath = Join-Path $baseDir "bin\Release\ClipClick.exe"

Get-Process ClipClick -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

$proc = Start-Process -FilePath $exePath -PassThru
Write-Host "Started ClipClick PID: $($proc.Id)"
Start-Sleep -Seconds 2

Add-Type -Path "bin\Release\System.Data.SQLite.dll"
[System.Reflection.Assembly]::LoadFrom($exePath) | Out-Null
$db = New-Object ClipClick.Services.DatabaseService

for ($k = 1; $k -le 3; $k++) {
    $text = "Live Copy Test Step $k - $(Get-Date -Format 'HH:mm:ss.fff')"
    Write-Host "Setting clipboard: $text"
    Set-Clipboard -Value $text
    Start-Sleep -Seconds 2
    $found = $db.GetClipboardEntries($text)
    Write-Host "Found in DB: $($found.Count)"
    if ($found.Count -gt 0) {
        Write-Host "  -> Content: $($found[0].Content)" -ForegroundColor Green
    } else {
        Write-Host "  -> Not found in DB!" -ForegroundColor Red
    }
}

$proc.Kill()
