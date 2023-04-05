// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Tooling.SampleGen.Tests.Helpers;

public record SourceGeneratorRunResult(Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics);
