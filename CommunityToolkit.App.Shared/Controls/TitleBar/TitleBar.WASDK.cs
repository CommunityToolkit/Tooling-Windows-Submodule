// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINAPPSDK
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace CommunityToolkit.App.Shared.Controls;

public partial class TitleBar : Control
{
    private AppWindow? appWindow;
    ColumnDefinition? LeftPaddingColumn;
    ColumnDefinition? BackButtonColumn;
    ColumnDefinition? MenuButtonColumn;
    ColumnDefinition? IconColumn;
    ColumnDefinition? TitleColumn;
    ColumnDefinition? SubtitleColumn;
    ColumnDefinition? LeftDragColumn;
    ColumnDefinition? ContentColumn;
    ColumnDefinition? FooterColumn;
    ColumnDefinition? RightDragColumn;
    ColumnDefinition? RightPaddingColumn;

    private void SetWASDKTitleBar()
    {
        appWindow = GetAppWindowForCurrentWindow();
        if (ConfigureTitleBar)
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            this.Loaded -= AppTitleBar_Loaded;
            this.SizeChanged -= AppTitleBar_SizeChanged;

            this.Loaded += AppTitleBar_Loaded;
            this.SizeChanged += AppTitleBar_SizeChanged;

            Window.Activated -= Window_Activated;
            Window.Activated += Window_Activated;

            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Set the width of padding columns in the UI.
            LeftPaddingColumn = (ColumnDefinition)_titleBar!.GetTemplateChild(PartLeftPaddingColumn);

            BackButtonColumn = (ColumnDefinition)_titleBar!.GetTemplateChild("BackButtonColumn");
            MenuButtonColumn = (ColumnDefinition)_titleBar!.GetTemplateChild("MenuButtonColumn");
            IconColumn = (ColumnDefinition)_titleBar!.GetTemplateChild("IconColumn");
            TitleColumn = (ColumnDefinition)_titleBar!.GetTemplateChild("TitleColumn");
            SubtitleColumn = (ColumnDefinition)_titleBar!.GetTemplateChild("SubtitleColumn");
            LeftDragColumn = (ColumnDefinition)_titleBar!.GetTemplateChild("LeftDragColumn");
            ContentColumn = (ColumnDefinition)_titleBar!.GetTemplateChild("ContentColumn");
            FooterColumn = (ColumnDefinition)_titleBar!.GetTemplateChild("FooterColumn");
            RightDragColumn = (ColumnDefinition)_titleBar!.GetTemplateChild("RightDragColumn");
            RightPaddingColumn = (ColumnDefinition)_titleBar!.GetTemplateChild(PartRightPaddingColumn);

            // Get caption button occlusion information.
            int CaptionButtonOcclusionWidthRight = appWindow.TitleBar.RightInset;
            int CaptionButtonOcclusionWidthLeft = appWindow.TitleBar.LeftInset;
            LeftPaddingColumn.Width = new GridLength(CaptionButtonOcclusionWidthLeft);
            RightPaddingColumn.Width = new GridLength(CaptionButtonOcclusionWidthRight);

            // A taller title bar is only supported when drawing a fully custom title bar
            if (AppWindowTitleBar.IsCustomizationSupported() && appWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                if (DisplayMode == DisplayMode.Tall)
                {
                    // Choose a tall title bar to provide more room for interactive elements 
                    // like search box or person picture controls.
                    appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
                }
                else
                {
                    appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
                }
                // Recalculate the drag region for the custom title bar 
                // if you explicitly defined new draggable areas.
                SetDragRegionForCustomTitleBar(appWindow);
            }
        }
        else
        {
            appWindow.TitleBar.ResetToDefault();
        }
    }

    private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
    {
        // Check to see if customization is supported.
        // The method returns true on Windows 10 since Windows App SDK 1.2, and on all versions of
        // Windows App SDK on Windows 11.
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            SetDragRegionForCustomTitleBar(appWindow!);
        }
    }

    private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Check to see if customization is supported.
        // The method returns true on Windows 10 since Windows App SDK 1.2, and on all versions of
        // Windows App SDK on Windows 11.
        if (AppWindowTitleBar.IsCustomizationSupported() && appWindow!.TitleBar.ExtendsContentIntoTitleBar)
        {
            // Update drag region if the size of the title bar changes.
            SetDragRegionForCustomTitleBar(appWindow);
        }
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            VisualStateManager.GoToState(this, WindowDeactivatedState, true);
        }
        else
        {
            VisualStateManager.GoToState(this, WindowActivatedState, true);
        }
    }

    private void SetDragRegionForCustomTitleBar(AppWindow appWindow)
    {
        // Check to see if customization is supported.
        // The method returns true on Windows 10 since Windows App SDK 1.2, and on all versions of
        // Windows App SDK on Windows 11.
        if (AppWindowTitleBar.IsCustomizationSupported() && appWindow.TitleBar.ExtendsContentIntoTitleBar)
        {
            double scaleAdjustment = GetScaleAdjustment();

            RightPaddingColumn!.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment);
            LeftPaddingColumn!.Width = new GridLength(appWindow.TitleBar.LeftInset / scaleAdjustment);

            List<Windows.Graphics.RectInt32> dragRectsList = new();

            Windows.Graphics.RectInt32 dragRectL;
            dragRectL.X = (int)((LeftPaddingColumn.ActualWidth + BackButtonColumn!.ActualWidth + MenuButtonColumn!.ActualWidth) * scaleAdjustment);
            dragRectL.Y = 0;
            dragRectL.Height = (int)(this.ActualHeight * scaleAdjustment);
            dragRectL.Width = (int)((IconColumn!.ActualWidth
                                        + TitleColumn!.ActualWidth
                                          + SubtitleColumn!.ActualWidth
                                        + LeftDragColumn!.ActualWidth) * scaleAdjustment);
            dragRectsList.Add(dragRectL);

            Windows.Graphics.RectInt32 dragRectR;
            dragRectR.X = (int)((LeftPaddingColumn.ActualWidth + IconColumn.ActualWidth + BackButtonColumn!.ActualWidth + MenuButtonColumn!.ActualWidth
                                + ((TextBlock)_titleBar!.GetTemplateChild("PART_TitleText")).ActualWidth
                                + ((TextBlock)_titleBar!.GetTemplateChild("PART_SubTitleText")).ActualWidth
                                + LeftDragColumn.ActualWidth
                                + ContentColumn!.ActualWidth) * scaleAdjustment);
            dragRectR.Y = 0;
            dragRectR.Height = (int)(this.ActualHeight * scaleAdjustment);
            dragRectR.Width = (int)(RightDragColumn!.ActualWidth * scaleAdjustment);
            dragRectsList.Add(dragRectR);

            Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

            appWindow.TitleBar.SetDragRectangles(dragRects);
        }
    }


    private AppWindow GetAppWindowForCurrentWindow()
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(Window);
        WindowId wndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(wndId);
    }

    [DllImport("Shcore.dll", SetLastError = true)]
    internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

    internal enum Monitor_DPI_Type : int
    {
        MDT_Effective_DPI = 0,
        MDT_Angular_DPI = 1,
        MDT_Raw_DPI = 2,
        MDT_Default = MDT_Effective_DPI
    }

    private double GetScaleAdjustment()
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this.Window);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
        IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

        // Get DPI.
        int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
        if (result != 0)
        {
            throw new Exception("Could not get DPI for monitor.");
        }

        uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
        return scaleFactorPercent / 100.0;
    }
}
#endif
