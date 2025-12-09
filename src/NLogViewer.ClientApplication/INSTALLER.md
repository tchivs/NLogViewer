# Installer Setup Guide

This document describes how to create installers for NLogViewer.ClientApplication.

## Build Configurations

The project supports two build configurations:

### 1. Self-Contained (All Dependencies)
- Includes .NET 8 runtime
- Larger size (~100-150 MB)
- No separate framework installation needed
- Single executable or folder

**Build Command:**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### 2. Framework-Dependent
- Requires .NET 8 runtime installed
- Smaller size (~10-20 MB)
- User must install .NET separately

**Build Command:**
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

## Installer Options

### Option 1: WiX Toolset (Recommended)

WiX (Windows Installer XML) is a professional installer toolkit that creates MSI installers.

#### Prerequisites
- Install WiX Toolset from https://wixtoolset.org/
- Visual Studio extension: WiX Toolset Visual Studio Extension

#### Basic WiX Setup
1. Create a new WiX project in the solution
2. Reference the ClientApplication output
3. Configure product information, shortcuts, and file associations
4. Build the MSI installer

#### Example WiX Product.wxs Structure
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="NLogViewer Client Application" Language="1033" Version="1.0.0.0" Manufacturer="Your Company" UpgradeCode="YOUR-GUID-HERE">
    <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
    
    <MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
    
    <MediaTemplate />
    
    <Feature Id="ProductFeature" Title="NLogViewer Client Application" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
    </Feature>
  </Product>
  
  <Fragment>
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="NLogViewer Client Application" />
      </Directory>
      <Directory Id="ProgramMenuFolder">
        <Directory Id="ApplicationProgramsFolder" Name="NLogViewer Client Application" />
      </Directory>
    </Directory>
  </Fragment>
  
  <Fragment>
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <Component Id="ApplicationFiles">
        <File Id="NLogViewerClientApplication.exe" Source="$(var.NLogViewer.ClientApplication.TargetPath)" KeyPath="yes" />
      </Component>
      <Component Id="ApplicationShortcut" Directory="ApplicationProgramsFolder">
        <Shortcut Id="ApplicationStartMenuShortcut" Name="NLogViewer Client Application" Description="NLog Log Viewer Client Application" Target="[INSTALLFOLDER]NLogViewer.ClientApplication.exe" WorkingDirectory="INSTALLFOLDER" />
        <RemoveFolder Id="ApplicationProgramsFolder" On="uninstall" />
        <RegistryValue Root="HKCU" Key="Software\NLogViewer\ClientApplication" Name="installed" Type="integer" Value="1" KeyPath="yes" />
      </Component>
    </ComponentGroup>
  </Fragment>
</Wix>
```

### Option 2: Inno Setup

Inno Setup is a free installer for Windows programs.

#### Prerequisites
- Download Inno Setup from https://jrsoftware.org/isinfo.php

#### Basic Inno Setup Script
```pascal
[Setup]
AppName=NLogViewer Client Application
AppVersion=1.0
DefaultDirName={pf}\NLogViewer Client Application
DefaultGroupName=NLogViewer Client Application
OutputDir=installer
OutputBaseFilename=NLogViewerClientApplication-Setup
Compression=lzma
SolidCompression=yes

[Files]
Source: "bin\Release\net8-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\NLogViewer Client Application"; Filename: "{app}\NLogViewer.ClientApplication.exe"
Name: "{commondesktop}\NLogViewer Client Application"; Filename: "{app}\NLogViewer.ClientApplication.exe"

[Run]
Filename: "{app}\NLogViewer.ClientApplication.exe"; Description: "Launch NLogViewer Client Application"; Flags: nowait postinstall skipifsilent
```

### Option 3: Portable Version

For a portable version, simply copy the published output folder to a USB drive or network location.

**Build Command:**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

The output folder can be run directly without installation.

## Build Scripts

### PowerShell Build Script (build-installer.ps1)
```powershell
# Build self-contained version
dotnet publish src/NLogViewer.ClientApplication/NLogViewer.ClientApplication.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish/self-contained

# Build framework-dependent version
dotnet publish src/NLogViewer.ClientApplication/NLogViewer.ClientApplication.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o publish/framework-dependent

Write-Host "Build completed. Output in publish/ directory"
```

## Notes

- For production installers, sign the executables with a code signing certificate
- Consider adding auto-update functionality
- Test installers on clean Windows systems
- Include .NET runtime installer in framework-dependent builds if needed

