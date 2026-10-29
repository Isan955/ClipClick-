@echo off
setlocal
echo ========================================================
echo               ClipClick Build Script
echo ========================================================

set MSBUILD="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

if not exist %MSBUILD% (
    echo [ERROR] MSBuild .NET Framework 4.8 tidak ditemukan di:
    echo %MSBUILD%
    pause
    exit /b 1
)

echo.
echo [1/2] Membangun ClipClick (Release)...
%MSBUILD% ClipClick.csproj /v:minimal /p:Configuration=Release /p:PlatformTarget=x64
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build Release gagal!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Membangun ClipClick (Debug)...
%MSBUILD% ClipClick.csproj /v:minimal /p:Configuration=Debug /p:PlatformTarget=x64
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build Debug gagal!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================================
echo       BUILD BERHASIL! File executable tersedia di:
echo       .\bin\Release\ClipClick.exe
echo       .\bin\Debug\ClipClick.exe
echo ========================================================
echo.
pause
