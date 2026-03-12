// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Linq;
using Windows.ApplicationModel;

#if WINUI2
using Windows.UI.Xaml.Media.Imaging;
#elif WINUI3
using Microsoft.UI.Xaml.Media.Imaging;
#endif

namespace CommunityToolkit.App.Shared.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public string AppVersion => $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";

    public string UnoVersion => $"{UnoPackageVariant} version {Assembly.GetExecutingAssembly()?.GetCustomAttribute<CommunityToolkit.Attributes.UnoPackageVersionAttribute>()?.Version.ToString() ?? "N/A"}";

    public string UnoPackageVariant => $"{WinUIMajorVersion switch { 2 => "Uno.UI", 3 => "Uno.WinUI", _ => throw new InvalidOperationException("Unknown WinUI version") }}";

    public uint WinUIMajorVersion =>
    #if WINUI2
        2;
    #elif WINUI3
        3;
    #else
        throw new InvalidOperationException("Unknown WinUI version");
    #endif

    public SettingsPage()
    {
        this.InitializeComponent();
    }
}
