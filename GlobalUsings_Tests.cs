// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file contains directives available in test projects.
// Learn more global using directives at https://docs.microsoft.com/dotnet/csharp/language-reference/keywords/using-directive#global-modifier

global using CommunityToolkit.Tests.Internal; // TODO: For CompositionTargetHelper until ported over into package.
global using CommunityToolkit.WinUI;

#if !WINAPPSDK
global using Windows.UI;
global using Windows.UI.Core;
#else
global using Microsoft.UI;
#endif

global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
