Write-Host "bin\Release\Data exists:" (Test-Path "bin\Release\Data")
Write-Host "LOCALAPPDATA\ClipClick exists:" (Test-Path "$env:LOCALAPPDATA\ClipClick")
if (Test-Path "$env:LOCALAPPDATA\ClipClick\clipclick.db") {
    Write-Host "Found DB in LOCALAPPDATA with size:" (Get-Item "$env:LOCALAPPDATA\ClipClick\clipclick.db").Length
}
if (Test-Path "bin\Release\Data\clipclick.db") {
    Write-Host "Found DB in bin\Release\Data with size:" (Get-Item "bin\Release\Data\clipclick.db").Length
}
