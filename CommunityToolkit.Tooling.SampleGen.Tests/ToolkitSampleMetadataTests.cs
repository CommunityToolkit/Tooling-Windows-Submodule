// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Tooling.SampleGen.Diagnostics;
using CommunityToolkit.Tooling.SampleGen.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommunityToolkit.Tooling.SampleGen.Tests;

[TestClass]
public partial class ToolkitSampleMetadataTests
{
    private const string SAMPLE_ASM_NAME = "MetadataTests.Samples";

    [TestMethod]
    public void SampleAttributeOnUnsupportedType()
    {
        var source = """
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {
                [ToolkitSample(id: nameof(Sample), "Test Sample", description: "")]
                public partial class Sample
                {
                }
            }
            """;

        var result = source.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME);

        result.AssertDiagnosticsAre(DiagnosticDescriptors.SampleAttributeOnUnsupportedType, DiagnosticDescriptors.SampleNotReferencedInMarkdown);
        result.AssertNoCompilationErrors();
    }

    [TestMethod]
    public void SampleOptionPaneAttributeOnUnsupportedType()
    {
        var source = """
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {
                [ToolkitSampleOptionsPane(sampleId: nameof(Sample))]
                public partial class SampleOptionsPane
                {
                }

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

        var result = source.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME);

        result.AssertDiagnosticsAre(DiagnosticDescriptors.SampleOptionPaneAttributeOnUnsupportedType, DiagnosticDescriptors.SampleNotReferencedInMarkdown);
        result.AssertNoCompilationErrors();
    }

    [TestMethod]
    public void SampleAttributeValid()
    {
        var source = """
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

        var result = source.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME);

        result.AssertDiagnosticsAre(DiagnosticDescriptors.SampleNotReferencedInMarkdown);
        result.AssertNoCompilationErrors();
    }
}
