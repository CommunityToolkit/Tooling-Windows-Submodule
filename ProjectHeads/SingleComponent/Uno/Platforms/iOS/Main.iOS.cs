// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Uno.UI.Hosting;
using CommunityToolkit.App.Shared;

namespace CommunityToolkit.App.Uno;

public class EntryPoint
{
    public static void Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new CommunityToolkit.App.Shared.App())
            .UseAppleUIKit()
            .Build();

        host.Run();
    }
}