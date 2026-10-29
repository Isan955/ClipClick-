$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$exePath = Join-Path $baseDir "bin\Release\ClipClick.exe"

Get-Process ClipClick -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

$proc = Start-Process -FilePath $exePath -PassThru
Write-Host "Started ClipClick PID: $($proc.Id)"
Start-Sleep -Seconds 2

for ($k = 1; $k -le 4; $k++) {
    Write-Host "--- Step $k ---"
    Write-Host "Process running: $(-not $proc.HasExited)"
    if ($proc.HasExited) {
        Write-Host "Process exited with code: $($proc.ExitCode)" -ForegroundColor Red
        break
    }
    $text = "Test-$k-" + (Get-Random)
    Set-Clipboard -Value $text
    Start-Sleep -Seconds 2
}

if (-not $proc.HasExited) { $proc.Kill() }
