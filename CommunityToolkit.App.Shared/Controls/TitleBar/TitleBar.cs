// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.ApplicationModel.Core;

namespace CommunityToolkit.App.Shared.Controls;

[TemplateVisualState(Name = BackButtonVisibleState, GroupName = BackButtonStates)]
[TemplateVisualState(Name = BackButtonCollapsedState, GroupName = BackButtonStates)]
[TemplateVisualState(Name = PaneButtonVisibleState, GroupName = PaneButtonStates)]
[TemplateVisualState(Name = PaneButtonCollapsedState, GroupName = PaneButtonStates)]
[TemplateVisualState(Name = WindowActivatedState, GroupName = ActivationStates)]
[TemplateVisualState(Name = WindowDeactivatedState, GroupName = ActivationStates)]
[TemplateVisualState(Name = StandardState, GroupName = DisplayModeStates)]
[TemplateVisualState(Name = TallState, GroupName = DisplayModeStates)]
[TemplatePart(Name = PartBackButton, Type = typeof(Button))]
[TemplatePart(Name = PartPaneButton, Type = typeof(Button))]
[TemplatePart(Name = PartDragRegionPresenter, Type = typeof(Grid))]
public partial class TitleBar : Control
{
    private const string PartLeftPaddingColumn = "LeftPaddingColumn";
    private const string PartRightPaddingColumn = "RightPaddingColumn";
    private const string PartDragRegionPresenter = "PART_DragRegion";
    private const string PartBackButton = "PART_BackButton";
    private const string PartPaneButton = "PART_PaneButton";

    private const string BackButtonVisibleState = "BackButtonVisible";
    private const string BackButtonCollapsedState = "BackButtonCollapsed";
    private const string BackButtonStates = "BackButtonStates";

    private const string PaneButtonVisibleState = "PaneButtonVisible";
    private const string PaneButtonCollapsedState = "PaneButtonCollapsed";
    private const string PaneButtonStates = "PaneButtonStates";

    private const string WindowActivatedState = "Activated";
    private const string WindowDeactivatedState = "Deactivated";
    private const string ActivationStates = "WindowActivationStates";

    private const string StandardState = "Standard";
    private const string TallState = "Tall";
    private const string DisplayModeStates = "DisplayModeStates";

    private Grid? _dragRegion;
    private TitleBar? _titleBar;
  
   

    public event EventHandler<RoutedEventArgs>? BackButtonClick;
    public event EventHandler<RoutedEventArgs>? PaneButtonClick;

    public TitleBar()
    {
        this.DefaultStyleKey = typeof(TitleBar);
    }

    protected override void OnApplyTemplate()
    {
        _titleBar = (TitleBar)this;

        if ((Button)_titleBar.GetTemplateChild(PartBackButton) is Button backButton)
        {
            backButton.Click -= BackButton_Click;
            backButton.Click += BackButton_Click;
        }

        if ((Button)_titleBar.GetTemplateChild(PartPaneButton) is Button paneButton)
        {
            paneButton.Click -= PaneButton_Click;
            paneButton.Click += PaneButton_Click;
        }
        _dragRegion = (Grid)_titleBar.GetTemplateChild(PartDragRegionPresenter);
        Update();
        SetTitleBar();
        base.OnApplyTemplate();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        OnBackButtonClicked();
    }

    private void PaneButton_Click(object sender, RoutedEventArgs e)
    {
        OnPaneButtonClicked();
    }

    private void OnBackButtonClicked()
    {
        BackButtonClick?.Invoke(this, new RoutedEventArgs());
    }

    private void OnPaneButtonClicked()
    {
        PaneButtonClick?.Invoke(this, new RoutedEventArgs());
    }
    private void Update()
    {
        VisualStateManager.GoToState(this, IsBackButtonVisible ? BackButtonVisibleState : BackButtonCollapsedState, true);
        VisualStateManager.GoToState(this, IsPaneButtonVisible ? PaneButtonVisibleState : PaneButtonCollapsedState, true);
    }

    private void SetTitleBar()
    {
#if WINDOWS_UWP && !HAS_UNO
        SetUWPTitleBar();
#endif

#if WINAPPSDK
        SetWASDKTitleBar();
#endif
    }
}
