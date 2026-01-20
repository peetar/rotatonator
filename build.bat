@echo off
echo Building Rotatonator...
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed.
    echo.
    echo Please download and install .NET 8.0 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo Restoring packages...
dotnet restore
if %errorlevel% neq 0 (
    echo Failed to restore packages
    pause
    exit /b 1
)

echo.
echo Building application...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo Build failed
    pause
    exit /b 1
)

echo.
echo Build successful!
echo.
echo Executable location: Rotatonator\bin\Release\net8.0-windows\Rotatonator.exe
echo.
pause
