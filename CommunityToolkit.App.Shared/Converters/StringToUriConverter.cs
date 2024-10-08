// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.App.Shared.Helpers;

namespace CommunityToolkit.App.Shared.Converters;

public partial class StringToUriConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return new Uri(IconHelper.GetIconPath((string)value));
    }

    public object ConvertBack(object value, Type targetType,  object parameter, string language)
    {
        return value;
    }
}
