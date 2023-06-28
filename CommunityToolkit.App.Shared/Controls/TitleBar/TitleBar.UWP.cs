// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINDOWS_UWP && !HAS_UNO
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI;

namespace CommunityToolkit.App.Shared.Controls;

public partial class TitleBar : Control
{
    private void SetUWPTitleBar()
    {
        if (ConfigureTitleBar)
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged -= this.TitleBar_LayoutMetricsChanged;
            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += this.TitleBar_LayoutMetricsChanged;
            Window.Current.Activated -= this.Current_Activated;
            Window.Current.Activated += this.Current_Activated;
            Window.Current.SetTitleBar(_dragRegion);
            ApplicationView.GetForCurrentView().TitleBar.ButtonBackgroundColor = Colors.Transparent;
            ApplicationView.GetForCurrentView().TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }
        else
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
            Window.Current.Activated -= this.Current_Activated;
            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged -= this.TitleBar_LayoutMetricsChanged;
            Window.Current.SetTitleBar(null);
        }
    }

    private void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
    {
        if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
        {
            VisualStateManager.GoToState(this, WindowDeactivatedState, true);
        }
        else
        {
            VisualStateManager.GoToState(this, WindowActivatedState, true);
        }
    }

    private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
    {
        if (_titleBar != null)
        {
            ColumnDefinition Left = (ColumnDefinition)_titleBar.GetTemplateChild(PartLeftPaddingColumn);
            ColumnDefinition Right = (ColumnDefinition)_titleBar.GetTemplateChild(PartRightPaddingColumn);
            Left.Width = new GridLength(CoreApplication.GetCurrentView().TitleBar.SystemOverlayLeftInset);
            Right.Width = new GridLength(CoreApplication.GetCurrentView().TitleBar.SystemOverlayRightInset);
        }
    }
}
#endif
