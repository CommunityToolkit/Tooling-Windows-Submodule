// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.App.Shared.Helpers;

#if WINAPPSDK
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
#else
using UnhandledExceptionEventArgs = Windows.UI.Xaml.UnhandledExceptionEventArgs;
#endif

namespace CommunityToolkit.App.Shared;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public sealed partial class App : Application
{
    // MacOS and iOS don't know the correct type without a full namespace declaration, confusing it with NSWindow and UIWindow.
    // 'using static' will not work.
#if WINUI3
    public static Microsoft.UI.Xaml.Window? currentWindow = Microsoft.UI.Xaml.Window.Current;
#elif WINUI2
    private static Windows.UI.Xaml.Window? currentWindow = Windows.UI.Xaml.Window.Current;
#endif

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        UnhandledException += this.App_UnhandledException;
    }

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        TrackingManager.TrackException(e.Exception);
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="e">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
#if WINDOWS_WINAPPSDK
        currentWindow = new Window();
        currentWindow.Title = "Windows Community Toolkit Gallery";
        currentWindow.AppWindow.SetIcon("Assets/Icon.ico");
        currentWindow.SystemBackdrop = new MicaBackdrop();
#if ALL_SAMPLES
        if (currentWindow is not null)
        {
            currentWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            currentWindow.AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        }
#endif
#endif

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (currentWindow is not null && currentWindow.Content is not Frame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            currentWindow.Content = new Frame();
        }

        if (currentWindow?.Content is Frame rootFrame)
        {
            rootFrame.NavigationFailed += OnNavigationFailed;

#if !WINAPPSDK
            if (e.PrelaunchActivated == false)
#endif
                rootFrame.Navigate(typeof(AppLoadingView), e.Arguments);
        }

        // Ensure the current window is active
        currentWindow?.Activate();
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
    }
}
