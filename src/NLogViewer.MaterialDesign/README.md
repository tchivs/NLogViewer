# NLogViewer.MaterialDesign

A Material Design themed version of the NLogViewer WPF control that provides a modern, beautiful interface for viewing NLog log entries in WPF applications.

## Overview

NLogViewer.MaterialDesign extends the base NLogViewer control with Material Design styling, providing:
- Modern Material Design UI components
- Beautiful log level icons and color coding
- Responsive layout with Material Design cards
- Consistent theming with Material Design principles

## Installation

### NuGet Package
```xml
<PackageReference Include="NLogViewer.MaterialDesign" Version="[version]" />
```

### Dependencies
- .NET 8.0 or higher
- MaterialDesignThemes (4.9.0 or higher)
- NLogViewer (base control)

## Quick Start

### 1. Add Required References

Add the following references to your project:

```xml
<PackageReference Include="MaterialDesignThemes" Version="4.9.0" />
<PackageReference Include="NLogViewer.MaterialDesign" Version="[version]" />
```

### 2. Configure Application Resources

In your `App.xaml`, add the Material Design theme and NLogViewer Material Design styles:

```xml
<Application x:Class="YourApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:xamlConverter="clr-namespace:DJ.XamlConverter;assembly=NLogViewer"
             xmlns:xamlMultiValueConverter="clr-namespace:DJ.XamlMultiValueConverter;assembly=NLogViewer"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Material Design Theme -->
                <materialDesign:BundledTheme BaseTheme="Light" PrimaryColor="Orange" SecondaryColor="Orange"/>
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign3.Defaults.xaml" />
                <!-- NLogViewer Material Design Styles -->
                <ResourceDictionary Source="pack://application:,,,/NLogViewer.MaterialDesign;component/Themes/MaterialDesign.NLogViewer.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```
