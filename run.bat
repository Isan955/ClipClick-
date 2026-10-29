@echo off
setlocal
if exist "bin\Release\ClipClick.exe" (
    start "" "bin\Release\ClipClick.exe"
    exit /b 0
)

if exist "bin\Debug\ClipClick.exe" (
    start "" "bin\Debug\ClipClick.exe"
    exit /b 0
)

echo [INFO] ClipClick.exe belum dibangun. Menjalankan build.bat terlebih dahulu...
call build.bat
if exist "bin\Release\ClipClick.exe" (
    start "" "bin\Release\ClipClick.exe"
)
