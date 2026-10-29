Add-Type -Path "bin\Release\System.Data.SQLite.dll"
[System.Reflection.Assembly]::LoadFrom("bin\Release\ClipClick.exe") | Out-Null
$db = New-Object ClipClick.Services.DatabaseService
$entries = $db.GetClipboardEntries("")
Write-Host "Total entries in DB: $($entries.Count)"
foreach ($e in $entries) {
    Write-Host "ID: $($e.Id) | Pinned: $($e.IsPinned) | Content: $($e.Content)"
}
