[Environment]::CurrentDirectory = (Join-Path (Get-Location) "bin\Release")
$binDir = Join-Path (Get-Location) "bin\Release"
Add-Type -Path (Join-Path $binDir "System.Data.SQLite.dll")
[System.Reflection.Assembly]::LoadFrom((Join-Path $binDir "ClipClick.exe")) | Out-Null
Write-Host "Storage Directory is:" ([ClipClick.Services.DatabaseService]::GetStorageDirectory())
