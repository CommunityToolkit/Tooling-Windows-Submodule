// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Tooling.SampleGen;

namespace CommunityToolkit.App.Shared.Helpers;

public static class IconHelper
{
    internal const string SourceAssetsPrefix = "ms-appx:///";
    internal const string FallBackControlIconPath = "ms-appx:///Assets/DefaultControlIcon.png";

    public static IconElement? GetCategoryIcon(ToolkitSampleCategory category)
    {
        IconElement? iconElement = null;
        switch (category)
        {
            case ToolkitSampleCategory.Layouts: iconElement = new FontIcon() { Glyph = "\uE138" }; break;
            case ToolkitSampleCategory.Controls: iconElement = new FontIcon() { Glyph = "\ue73a" }; break;
            case ToolkitSampleCategory.Animations: iconElement = new FontIcon() { Glyph = "\ue945" }; break;
            case ToolkitSampleCategory.Extensions: iconElement = new FontIcon() { Glyph = "\ue95f" }; break;
            case ToolkitSampleCategory.Helpers: iconElement = new FontIcon() { Glyph = "\ue946" }; break;
            case ToolkitSampleCategory.Xaml: iconElement = new FontIcon() { Glyph = "\ue8af" }; break;
        }
        return iconElement;
    }

    public static string GetIconPath(string? IconPath)
    {
        if (!string.IsNullOrEmpty(IconPath))
        {
            return SourceAssetsPrefix + IconPath;
        }
        else
        {
            return FallBackControlIconPath;
        }
    }
}
