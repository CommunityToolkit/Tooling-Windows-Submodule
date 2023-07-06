// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CommunityToolkit.Tooling.SampleGen.Tests.Helpers;

internal class InMemoryAdditionalText : AdditionalText
{
    private readonly SourceText _content;

    public InMemoryAdditionalText(string path, string content)
    {
        Path = path;
        _content = SourceText.From(content, Encoding.UTF8);
    }

    public override string Path { get; }

    public override SourceText GetText(CancellationToken cancellationToken = default) => _content;
}
