$binDir = Join-Path (Get-Location) "bin\Release"
Add-Type -Path (Join-Path $binDir "System.Data.SQLite.dll")
[System.Reflection.Assembly]::LoadFrom((Join-Path $binDir "ClipClick.exe")) | Out-Null
$dir = [ClipClick.Services.DatabaseService]::GetStorageDirectory()
Write-Host "Dir: $dir"
Get-ChildItem -Path $dir | Select-Object Name, Length, LastWriteTime
