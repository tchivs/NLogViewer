# NLogViewer Client Application Installer

This directory contains the WiX installer project for NLogViewer Client Application.

## Prerequisites

1. **WiX Toolset v3.11 or newer** must be installed
   - Download from: https://wixtoolset.org/releases/
   - Or install via Visual Studio Installer (WiX Toolset Visual Studio Extension)

2. **Visual Studio** with WiX extension (recommended)
   - Or use WiX command-line tools

## Building the Installer

### Using Visual Studio
1. Open the solution in Visual Studio
2. Right-click on `NLogViewer.ClientApplication.Installer.wixproj`
3. Select "Build" or "Rebuild"

### Using Command Line
```powershell
msbuild installer\NLogViewer.ClientApplication.Installer.wixproj /p:Configuration=Release
```

## Output

The installer MSI file will be generated in:
- `installer\bin\Release\NLogViewer.ClientApplication.msi`

## Configuration

### Product Information
- **Product Name**: NLogViewer Client Application
- **Version**: 1.0.0.0 (update in Product.wxs)
- **Manufacturer**: Dominic Böxler
- **Upgrade Code**: A1B2C3D4-E5F6-7890-ABCD-EF1234567890

### Installation Features
- Installs to Program Files
- Creates Start Menu shortcut
- Creates Desktop shortcut (optional)
- Registry entries for uninstallation

### Customization

To modify the installer:
1. Edit `Product.wxs` for product information, directories, and components
2. Edit `NLogViewer.ClientApplication.Installer.wixproj` for build configuration
3. Add custom UI dialogs if needed
4. Add file associations if needed

## Notes

- The installer references the published output of NLogViewer.ClientApplication
- Ensure the application is built before building the installer
- For self-contained deployments, publish the application first:
  ```powershell
  dotnet publish app\NLogViewer.ClientApplication\NLogViewer.ClientApplication.csproj -c Release -r win-x64 --self-contained true
  ```


