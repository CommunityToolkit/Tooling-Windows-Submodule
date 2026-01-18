// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using CommunityToolkit.Tooling.SampleGen.Metadata;

namespace CommunityToolkit.App.Shared.Helpers;

public static class NavigationViewHelper
{
    public static IEnumerable<MUXC.NavigationViewItem> GenerateNavItemTree(IEnumerable<ToolkitFrontMatter> sampleMetadata)
    {
        // Make categories
        var categoryData = GenerateCategoryNavItems(sampleMetadata);

        foreach (var navData in categoryData)
        {
            var samplesBySubcategory = navData.SampleMetadata!.GroupBy(x => x.Subcategory)
                                             .OrderBy(g => g.Key.ToString());

            foreach (var subcategoryGroup in samplesBySubcategory)
            {
                navData.NavItem.MenuItems.Add(new MUXC.NavigationViewItemHeader() { Content = subcategoryGroup.Key.ToString() });
                foreach (var sampleNavItem in GenerateSampleNavItems(subcategoryGroup))
                {
                    navData.NavItem.MenuItems.Add(sampleNavItem);
                }
            }

            yield return navData.NavItem;
        }
    }

    private static IEnumerable<MUXC.NavigationViewItem> GenerateSampleNavItems(IEnumerable<ToolkitFrontMatter> sampleMetadata)
    {
        foreach (var metadata in sampleMetadata.OrderBy(meta => meta.Title))
        {
            MUXC.NavigationViewItem navItem = new MUXC.NavigationViewItem
            {
                Content = metadata.Title,
                Icon = new BitmapIcon()
                {
                    ShowAsMonochrome = false,
                    UriSource = new Uri(IconHelper.GetIconPath(metadata.Icon))
                },
                Tag = metadata,
            };

            // Check if this is a Labs component
            if (metadata.IsExperimental == true)
            {
                navItem.InfoBadge = new MUXC.InfoBadge() { Style = (Style)App.Current.Resources["LabsIconBadgeStyle"] };
            }
            yield return navItem;
        }
    }

    private static IEnumerable<GroupNavigationItemData> GenerateCategoryNavItems(IEnumerable<ToolkitFrontMatter> sampleMetadata)
    {
        var samplesByCategory = sampleMetadata.GroupBy(x => x.Category)
                                              .OrderBy(g => g.Key.ToString());

        foreach (var categoryGroup in samplesByCategory)
        {
            yield return new GroupNavigationItemData(new MUXC.NavigationViewItem
            {
                Content = categoryGroup.Key,
                Icon = IconHelper.GetCategoryIcon(categoryGroup.Key),
                SelectsOnInvoked = false,
            }, categoryGroup.ToArray());
        }
    }

    /// <param name="NavItem">A navigation item to contain items in this group.</param>
    /// <param name="SampleMetadata">The samples that belong under <see cref="NavItem"/>.</param>
    private record GroupNavigationItemData(MUXC.NavigationViewItem NavItem, IEnumerable<ToolkitFrontMatter> SampleMetadata);
}
