# Rotatonator Development & Build Instructions

## Prerequisites

### Required Software
1. **.NET 8.0 SDK** - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Choose the SDK (not just runtime)
   - Windows x64 installer recommended

2. **Visual Studio 2022** (Optional, but recommended for development)
   - Community Edition (free): https://visualstudio.microsoft.com/downloads/
   - Workloads needed:
     - .NET desktop development
     - Windows Presentation Foundation (WPF)

## Building the Application

### Option 1: Using Build Scripts (Easiest)

**Windows Command Prompt / PowerShell:**
```batch
build.bat
```

**PowerShell:**
```powershell
.\build.ps1
```

### Option 2: Using .NET CLI

```powershell
# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Build (Release)
dotnet build -c Release

# Run directly
dotnet run --project Rotatonator
```

### Option 3: Using Visual Studio

1. Open `Rotatonator.sln` in Visual Studio
2. Press `Ctrl+Shift+B` to build
3. Press `F5` to run with debugging

## Output Location

After building, the executable will be located at:
- **Debug**: `Rotatonator\bin\Debug\net8.0-windows\Rotatonator.exe`
- **Release**: `Rotatonator\bin\Release\net8.0-windows\Rotatonator.exe`

## Project Structure

```
rotatonator/
├── Rotatonator.sln              # Visual Studio solution file
├── build.bat                    # Windows build script
├── build.ps1                    # PowerShell build script
├── README.md                    # User documentation
├── BUILD.md                     # This file
└── Rotatonator/                 # Main project directory
    ├── Rotatonator.csproj       # Project file
    ├── App.xaml                 # Application definition
    ├── App.xaml.cs
    ├── MainWindow.xaml          # Main configuration window
    ├── MainWindow.xaml.cs
    ├── OverlayWindow.xaml       # Transparent overlay
    ├── OverlayWindow.xaml.cs
    ├── Models/
    │   └── RotationConfig.cs    # Configuration model
    └── Services/
        ├── LogMonitor.cs        # EQ log file monitor
        ├── RotationManager.cs   # Rotation logic
        └── KeyboardAutomation.cs # Keyboard input
```

## Development

### Running in Development Mode

```powershell
cd Rotatonator
dotnet run
```

### Adding NuGet Packages

```powershell
cd Rotatonator
dotnet add package PackageName
```

### Debugging

- Use Visual Studio for full debugging experience
- Set breakpoints in .cs files
- Use Debug > Start Debugging (F5)

## Distribution

To create a distributable version:

```powershell
# Create self-contained executable (includes .NET runtime)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Output will be in: Rotatonator\bin\Release\net8.0-windows\win-x64\publish\
```

This creates a single .exe file that can run on Windows without requiring .NET installation.

## Troubleshooting

### "dotnet command not found"
- Install .NET 8.0 SDK from the link above
- Restart your terminal/PowerShell after installation

### "The project file could not be loaded"
- Ensure you're in the correct directory
- Check that `Rotatonator.sln` exists

### Build errors
- Run `dotnet restore` first
- Check that all files are present
- Ensure .NET 8.0 SDK is installed (not just runtime)

## Testing

### Manual Testing Checklist

1. **Log File Selection**
   - Browse button works
   - Auto-detects EQ log directory if present

2. **Chain Configuration**
   - Can add/remove healers
   - Player name validation works

3. **Overlay**
   - Shows transparent window
   - Click-through works
   - Timers update correctly

4. **Log Monitoring**
   - Detects "begins to cast a spell" lines
   - Correctly identifies healers in rotation
   - Ignores non-rotation healers

5. **Audio/Auto-cast**
   - Audio beep plays on player turn
   - Hotkey sends correctly (test carefully!)

### Test with Sample Log

Create a test log file with entries like:
```
[Mon Jan 19 14:30:45 2026] Healer1 begins to cast a spell.
[Mon Jan 19 14:30:51 2026] Healer2 begins to cast a spell.
[Mon Jan 19 14:30:57 2026] Healer3 begins to cast a spell.
```

Monitor the file and verify rotation tracking works correctly.
