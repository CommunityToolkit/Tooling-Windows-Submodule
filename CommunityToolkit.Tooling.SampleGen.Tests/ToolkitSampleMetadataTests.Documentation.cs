// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Tooling.SampleGen.Diagnostics;
using CommunityToolkit.Tooling.SampleGen.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommunityToolkit.Tooling.SampleGen.Tests;

public partial class ToolkitSampleMetadataTests
{
    // We currently need at least one sample to test the document registry, so we'll have this for the base cases to share.
    private static readonly string SimpleSource = """
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {

                [ToolkitSample(id: nameof(Sample), "Test Sample", description: "")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {
                }
            }

            namespace Windows.UI.Xaml.Controls
            {
                public class UserControl { }
            }
            """;

    [TestMethod]
    public void MissingFrontMatterSection()
    {
        string markdown = @"
            # This is some test documentation...
            Without any front matter.
            ";

        var result = SimpleSource.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME, markdown);

        result.AssertNoCompilationErrors();
        result.AssertDiagnosticsAre(DiagnosticDescriptors.MarkdownYAMLFrontMatterException, DiagnosticDescriptors.SampleNotReferencedInMarkdown);
    }

    [DataRow(1, DisplayName = "Title")]
    [DataRow(3, DisplayName = "Description")]
    [DataRow(4, DisplayName = "Keywords")]
    [DataRow(7, DisplayName = "Category")]
    [DataRow(8, DisplayName = "Subcategory")]
    [DataRow(9, DisplayName = "GitHub Discussion Id")]
    [DataRow(10, DisplayName = "GitHub Issue Id")]
    [DataRow(10, DisplayName = "Icon")]
    [TestMethod]
    public void MissingFrontMatterField(int removeline)
    {
        string markdown = @"---
title: Canvas Layout
author: mhawker
description: A canvas-like VirtualizingLayout for use in an ItemsRepeater
keywords: CanvasLayout, ItemsRepeater, VirtualizingLayout, Canvas, Layout, Panel, Arrange
dev_langs:
    - csharp
category: Controls
subcategory: Layout
discussion-id: 0
issue-id: 0
icon: assets/icon.png
---
# This is some test documentation...
> [!SAMPLE Sample]
Without any front matter.";

        // Remove the field we want to test is missing.
        var lines = markdown.Split('\n').ToList();
        lines.RemoveAt(removeline);
        markdown = String.Join('\n', lines);

        var result = SimpleSource.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME, markdown);

        // We won't see the sample reference as we bail out when the front matter fails to be complete
        result.AssertNoCompilationErrors();
        result.AssertDiagnosticsAre(DiagnosticDescriptors.MarkdownYAMLFrontMatterMissingField, DiagnosticDescriptors.SampleNotReferencedInMarkdown);
    }

    [TestMethod]
    public void MarkdownInvalidSampleReference()
    {
        string markdown = @"---
title: Canvas Layout
author: mhawker
description: A canvas-like VirtualizingLayout for use in an ItemsRepeater
keywords: CanvasLayout, ItemsRepeater, VirtualizingLayout, Canvas, Layout, Panel, Arrange
dev_langs:
    - csharp
category: Controls
subcategory: Layout
discussion-id: 0
issue-id: 0
icon: assets/icon.png
---
# This is some test documentation...
> [!SAMPLE SampINVALIDle]
Without any front matter.
";

        var result = SimpleSource.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME, markdown);

        result.AssertNoCompilationErrors();
        result.AssertDiagnosticsAre(DiagnosticDescriptors.MarkdownSampleIdNotFound, DiagnosticDescriptors.SampleNotReferencedInMarkdown);
    }

    [TestMethod]
    public void DocumentationMissingSample()
    {
        string markdown = @"---
title: Canvas Layout
author: mhawker
description: A canvas-like VirtualizingLayout for use in an ItemsRepeater
keywords: CanvasLayout, ItemsRepeater, VirtualizingLayout, Canvas, Layout, Panel, Arrange
dev_langs:
    - csharp
category: Controls
subcategory: Layout
discussion-id: 0
issue-id: 0
icon: assets/icon.png
---
# This is some test documentation...
Without any sample.";

        var result = SimpleSource.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME, markdown);

        result.AssertNoCompilationErrors();
        result.AssertDiagnosticsAre(DiagnosticDescriptors.DocumentationHasNoSamples, DiagnosticDescriptors.SampleNotReferencedInMarkdown);
    }

    [TestMethod]
    public void DocumentationValidNoDiagnostics()
    {
        string markdown = @"---
title: Canvas Layout
author: mhawker
description: A canvas-like VirtualizingLayout for use in an ItemsRepeater
keywords: CanvasLayout, ItemsRepeater, VirtualizingLayout, Canvas, Layout, Panel, Arrange
dev_langs:
    - csharp
category: Controls
subcategory: Layout
discussion-id: 0
issue-id: 0
icon: assets/icon.png
---
# This is some test documentation...
Which is valid.
> [!SAMPLE Sample]";


        var result = SimpleSource.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME, markdown);

        result.AssertNoCompilationErrors();
        result.AssertDiagnosticsAre();
    }

    [TestMethod]
    public void DocumentationInvalidDiscussionId()
    {
        string markdown = @"---
title: Canvas Layout
author: mhawker
description: A canvas-like VirtualizingLayout for use in an ItemsRepeater
keywords: CanvasLayout, ItemsRepeater, VirtualizingLayout, Canvas, Layout, Panel, Arrange
dev_langs:
    - csharp
category: Controls
subcategory: Layout
discussion-id: https://github.com/1234
issue-id: 0
icon: assets/icon.png
---
# This is some test documentation...
Without an invalid discussion-id.";

        var result = string.Empty.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME, markdown);

        result.AssertNoCompilationErrors();
        result.AssertDiagnosticsAre(DiagnosticDescriptors.MarkdownYAMLFrontMatterException, DiagnosticDescriptors.DocumentationHasNoSamples);

        var diag = result.Diagnostics.First((d) => d.Descriptor == DiagnosticDescriptors.MarkdownYAMLFrontMatterException);
        Assert.IsTrue(diag.GetMessage().Contains("discussion-id"));
    }

    [TestMethod]
    public void DocumentationInvalidIssueId()
    {
        string markdown = @"---
title: Canvas Layout
author: mhawker
description: A canvas-like VirtualizingLayout for use in an ItemsRepeater
keywords: CanvasLayout, ItemsRepeater, VirtualizingLayout, Canvas, Layout, Panel, Arrange
dev_langs:
    - csharp
category: Controls
subcategory: Layout
discussion-id: 0
issue-id: https://github.com/1234
icon: assets/icon.png
---
# This is some test documentation...
Without an invalid issue-id.";

        var result = string.Empty.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME, markdown);

        result.AssertNoCompilationErrors();
        result.AssertDiagnosticsAre(DiagnosticDescriptors.MarkdownYAMLFrontMatterException, DiagnosticDescriptors.DocumentationHasNoSamples);

        var diag = result.Diagnostics.First((d) => d.Descriptor == DiagnosticDescriptors.MarkdownYAMLFrontMatterException);
        Assert.IsTrue(diag.GetMessage().Contains("issue-id"));
    }

    [TestMethod]
    public void DocumentationInvalidIsExperimental()
    {
        string markdown = @"---
title: Canvas Layout
author: mhawker
description: A canvas-like VirtualizingLayout for use in an ItemsRepeater
keywords: CanvasLayout, ItemsRepeater, VirtualizingLayout, Canvas, Layout, Panel, Arrange
dev_langs:
    - csharp
category: Controls
subcategory: Layout
discussion-id: 0
issue-id: 0
icon: assets/icon.png
experimental: No
---
# This is some test documentation...
Without an invalid experimental value.";

        var result = string.Empty.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME, markdown);

        result.AssertNoCompilationErrors();
        result.AssertDiagnosticsAre(DiagnosticDescriptors.MarkdownYAMLFrontMatterException, DiagnosticDescriptors.DocumentationHasNoSamples);

        var diag = result.Diagnostics.First((d) => d.Descriptor == DiagnosticDescriptors.MarkdownYAMLFrontMatterException);
        Assert.IsTrue(diag.GetMessage().Contains("experimental"));
    }

    [TestMethod]
    public void DocumentationValidWithRegistry()
    {
        string markdown = @"---
title: Canvas Layout
author: mhawker
description: A canvas-like VirtualizingLayout for use in an ItemsRepeater
keywords: CanvasLayout, ItemsRepeater, VirtualizingLayout, Canvas, Layout, Panel, Arrange
dev_langs:
    - csharp
category: Controls
subcategory: Layout
discussion-id: 0
issue-id: 0
icon: assets/icon.png
experimental: true
---
# This is some test documentation...
Which is valid.
> [!SAMPLE Sample]";

        var sampleProjectAssembly = SimpleSource.ToSyntaxTree()
            .CreateCompilation("MyApp.Samples")
            .ToMetadataReference();

        var headCompilation = string.Empty
            .ToSyntaxTree()
            .CreateCompilation("MyApp.Head")
            .AddReferences(sampleProjectAssembly);

        var result = headCompilation.RunSourceGenerator<ToolkitSampleMetadataGenerator>(markdown);

        result.AssertNoCompilationErrors();

        Assert.AreEqual(result.Compilation.GetFileContentsByName("ToolkitDocumentRegistry.g.cs"), """
        #nullable enable
        namespace CommunityToolkit.Tooling.SampleGen;

        public static class ToolkitDocumentRegistry
        {
            public static System.Collections.Generic.IEnumerable<CommunityToolkit.Tooling.SampleGen.Metadata.ToolkitFrontMatter> Execute()
            {
                yield return new CommunityToolkit.Tooling.SampleGen.Metadata.ToolkitFrontMatter() { Title = "Canvas Layout", Author = "mhawker", Description = "A canvas-like VirtualizingLayout for use in an ItemsRepeater", Keywords = "CanvasLayout, ItemsRepeater, VirtualizingLayout, Canvas, Layout, Panel, Arrange", Category = ToolkitSampleCategory.Controls, Subcategory = ToolkitSampleSubcategory.Layout, DiscussionId = 0, IssueId = 0, Icon = @"experiment/samples/assets/icon.png", FilePath = @"experiment\samples\documentation.md", SampleIdReferences = new string[] { "Sample" }, IsExperimental = true };
            }
        }
        """, "Unexpected code generated");
    }
}
