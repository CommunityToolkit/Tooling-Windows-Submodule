# CommunityToolkit MultiTarget System
Simplified targeting, packaging and deployment of an application and it's supporting libraries.

> **Note** The MultiTarget system is not designed to be used outside the Windows Community Toolkit or Toolkit Labs.

## MultiTarget MSBuild Property

`<MultiTarget>` is a custom property that indicates which target a component is designed to be built for / run on.

The supplied targets are used to create project references, generate solution files, enable/disable TargetFrameworks, build nuget packages, and more.

### Basic usage

Create a `MultiTarget.props` file in the root of your source project to change the platform targets for your component. This will be picked up automatically by your sample project, unless it has a `MultiTarget.props` of its own defined.

By default, all available targets are enabled:
```xml
<Project>
    <PropertyGroup>
        <MultiTarget>uwp;wasdk;wpf;win32;wasm;linux;macos;ios;android;</MultiTarget>
    </PropertyGroup>
</Project>
```

For example, to only target UWP, WASM and Android:

```xml
<Project>
    <PropertyGroup>
        <MultiTarget>uwp;wasm;android</MultiTarget>
    </PropertyGroup>
</Project>
```

### Available targets

A MultiTarget names a *platform your component runs on*, not a TFM. The TFMs and the deployable
head it maps to depend on which WinUI version is selected, since Uno 5.x (WinUI 2, `Uno.UI`) and
Uno 6.x (WinUI 3, `Uno.WinUI`) ship different heads.

| MultiTarget | Platform | WinUI 2 (Uno 5.x) | WinUI 3 (Uno 6.x) |
| --- | --- | --- | --- |
| `uwp` | Windows, UWP XAML | `uap10.0.17763`, `net{x}-windows10.0.26100` | n/a |
| `wasdk` | Windows, Windows App SDK | n/a | `net{x}-windows10.0.19041` |
| `wasm` | WebAssembly | `net{x}`, Wasm head | `net{x}`, `net{x}-browserwasm` head |
| `wpf` | Windows desktop, Skia WPF | `net{x}`, Skia WPF head | n/a — use `win32` |
| `win32` | Windows desktop, Skia Win32 | n/a — use `wpf` | `net{x}`, `net{x}-desktop` head |
| `linux` | Linux desktop | `net{x}`, Skia GTK head | `net{x}`, `net{x}-desktop` head |
| `macos` | macOS | `net{x}-maccatalyst` | `net{x}`, `net{x}-desktop` head |
| `ios` | iOS | `net{x}-ios` | `net{x}-ios` |
| `android` | Android | `net{x}-android` | `net{x}-android` |
| `netstandard` | .NET Standard, no WinUI | `netstandard2.0` | `netstandard2.0` |

`win32`, `linux` and `macos` all deploy from the same build output on WinUI 3: Uno 6 replaced the
per-OS Skia heads with a single cross-platform `net{x}-desktop` head that picks its backend at
runtime. They stay separate MultiTargets so a component can declare — and solution generation and
CI can request — only the desktop platforms that are actually supported.

Mac Catalyst is WinUI 2 only. Uno 7 removes support for it, and on WinUI 3 macOS is reached through
the Skia desktop head instead, so `net{x}-maccatalyst` is not part of the WinUI 3 package surface.

## ProjectReference Generation

The script `GenerateAllProjectReferences.ps1` will scan for toolkit components and generate `.props` files for each.

## NuGet Packages

The `<MultiTarget>` property is used to define the `TargetFrameworks` supported by that project. Projects packed into a NuGet packages will reflect this. 
