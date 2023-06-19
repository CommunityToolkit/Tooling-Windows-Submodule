// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Windows.ApplicationModel.Core;

namespace CommunityToolkit.App.Shared.Controls;

[TemplateVisualState(Name = BackButtonVisibleState, GroupName = BackButtonStates)]
[TemplateVisualState(Name = BackButtonCollapsedState, GroupName = BackButtonStates)]
[TemplateVisualState(Name = PaneButtonVisibleState, GroupName = PaneButtonStates)]
[TemplateVisualState(Name = PaneButtonCollapsedState, GroupName = PaneButtonStates)]
[TemplatePart(Name = PartBackButton, Type = typeof(Button))]
[TemplatePart(Name = PartPaneButton, Type = typeof(Button))]
[TemplatePart(Name = PartDragRegionPresenter, Type = typeof(Grid))]
public partial class TitleBar : Control
{
    private const string PartDragRegionPresenter = "PART_DragRegion";
    private const string PartBackButton = "PART_BackButton";
    private const string PartPaneButton = "PART_PaneButton";

    private const string BackButtonVisibleState = "BackButtonVisible";
    private const string BackButtonCollapsedState = "BackButtonCollapsed";
    private const string BackButtonStates = "BackButtonStates";

    private const string PaneButtonVisibleState = "PaneButtonVisible";
    private const string PaneButtonCollapsedState = "PaneButtonCollapsed";
    private const string PaneButtonStates = "PaneButtonStates";


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
#if WINAPPSDK && !HAS_UNO
        Window window = App.currentWindow;
        window.ExtendsContentIntoTitleBar = true;
        window.SetTitleBar(_dragRegion);
        // TO DO: BACKGROUND IS NOT TRANSPARENT
#endif
#if WINDOWS_UWP && !HAS_UNO
        Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
        Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += this.TitleBar_LayoutMetricsChanged;
        Window.Current.SetTitleBar(_dragRegion);
        // NOT SUPPORTED IN UNO WASM
#endif
    }

    private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
    {
        if (_titleBar != null)
        {
            ColumnDefinition Left = (ColumnDefinition)_titleBar.GetTemplateChild("LeftPaddingColumn");
            ColumnDefinition Right = (ColumnDefinition)_titleBar.GetTemplateChild("RightPaddingColumn");
            Left.Width = new GridLength(CoreApplication.GetCurrentView().TitleBar.SystemOverlayLeftInset);
            Right.Width = new GridLength(CoreApplication.GetCurrentView().TitleBar.SystemOverlayRightInset);
        }
    }
}
