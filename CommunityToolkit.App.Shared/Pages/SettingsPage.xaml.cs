// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Windows.ApplicationModel;

namespace CommunityToolkit.App.Shared.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public string AppVersion => $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";

    public string UnoVersion =>
        AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Uno.UI" || a.GetName().Name == "Uno.WinUI")?
            .GetName().Version?.ToString() ?? "N/A";

    public SettingsPage()
    {
        this.InitializeComponent();
    }
}
