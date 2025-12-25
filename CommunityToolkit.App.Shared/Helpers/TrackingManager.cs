// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if !DEBUG && WINDOWS_UWP && !HAS_UNO
// See https://learn.microsoft.com/windows/uwp/monetize/log-custom-events-for-dev-center
using Microsoft.Services.Store.Engagement;
#endif

namespace CommunityToolkit.App.Shared.Helpers;

#if DEBUG || !WINDOWS_UWP || HAS_UNO
// Stub for non-UWP platforms and DEBUG mode
internal class StoreServicesCustomEventLogger
{
    public static StoreServicesCustomEventLogger GetDefault() => new();

    public void Log(string message)
    {
        Debug.WriteLine($"Log - {message}");
    }
}
#endif

public static class TrackingManager
{
    private static StoreServicesCustomEventLogger? logger;

    static TrackingManager()
    {
        try
        {
            logger = StoreServicesCustomEventLogger.GetDefault();
        }
        catch
        {
            // Ignoring error
        }
    }

    public static void TrackException(Exception ex)
    {
        try
        {
            logger?.Log($"exception - {ex.Message} - {ex.StackTrace}");
        }
        catch
        {
            // Ignore error
        }
    }

    public static void TrackSample(string name)
    {
        try
        {
            logger?.Log($"sample - {name}");
        }
        catch
        {
            // Ignore error
        }
    }

    public static void TrackPage(string pageName)
    {
        try
        {
            logger?.Log($"openpage - {pageName}");
        }
        catch
        {
            // Ignore error
        }
    }
}
