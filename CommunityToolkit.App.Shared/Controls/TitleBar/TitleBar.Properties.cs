// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.App.Shared.Controls;

public partial class TitleBar : Control
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(TitleBar), new PropertyMetadata(default(ImageSource)));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(TitleBar), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(TitleBar), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(object), typeof(TitleBar), new PropertyMetadata(null));

    public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(nameof(Footer), typeof(object), typeof(TitleBar), new PropertyMetadata(null));

    public static readonly DependencyProperty IsBackButtonVisibleProperty = DependencyProperty.Register(nameof(IsBackButtonVisible), typeof(bool), typeof(TitleBar), new PropertyMetadata(false, IsBackButtonVisibleChanged));

    public static readonly DependencyProperty IsPaneButtonVisibleProperty = DependencyProperty.Register(nameof(IsPaneButtonVisible), typeof(bool), typeof(TitleBar), new PropertyMetadata(false, IsPaneButtonVisibleChanged));

    public static readonly DependencyProperty ConfigureTitleBarProperty = DependencyProperty.Register(nameof(ConfigureTitleBar), typeof(bool), typeof(TitleBar), new PropertyMetadata(true, ConfigureTitleBarChanged));

    public static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register(nameof(DisplayMode), typeof(DisplayMode), typeof(TitleBar), new PropertyMetadata(DisplayMode.Standard, DisplayModeChanged));

#if WINAPPSDK
   public static readonly DependencyProperty WindowProperty = DependencyProperty.Register(nameof(Window), typeof(Window), typeof(TitleBar), new PropertyMetadata(null));
#endif

    public ImageSource Icon
    {
        get => (ImageSource)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public object Content
    {
        get => (object)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public object Footer
    {
        get => (object)GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    public DisplayMode DisplayMode
    {
        get => (DisplayMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public bool IsBackButtonVisible
    {
        get => (bool)GetValue(IsBackButtonVisibleProperty);
        set => SetValue(IsBackButtonVisibleProperty, value);
    }

    public bool IsPaneButtonVisible
    {
        get => (bool)GetValue(IsPaneButtonVisibleProperty);
        set => SetValue(IsPaneButtonVisibleProperty, value);
    }

    public bool ConfigureTitleBar
    {
        get => (bool)GetValue(ConfigureTitleBarProperty);
        set => SetValue(ConfigureTitleBarProperty, value);
    }

#if WINAPPSDK
    public Window Window
    {
        get => (Window)GetValue(WindowProperty);
        set => SetValue(WindowProperty, value);
    }
#endif

    private static void IsBackButtonVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TitleBar)d).Update();
    }

    private static void IsPaneButtonVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TitleBar)d).Update();
    }

    private static void ConfigureTitleBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TitleBar)d).SetTitleBar();
    }

    private static void DisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TitleBar)d).SetTitleBar();
    }
}

public enum DisplayMode
{
    Standard,
    Tall
}
