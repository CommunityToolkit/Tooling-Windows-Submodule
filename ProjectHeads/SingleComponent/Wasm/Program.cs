// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.App.Shared;

#if WINAPPSDK
using Uno.UI.Hosting;
#endif

namespace ProjectTemplateExperiment.Samples.Wasm;

public class Program
{
#if WINAPPSDK
    static async Task Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
#else
    private static App? _app;

    static int Main(string[] args)
    {
        Application.Start(_ => _app = new App());

        return 0;
    }
#endif
}
