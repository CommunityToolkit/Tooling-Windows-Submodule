// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Tooling.SampleGen.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;

namespace CommunityToolkit.Tooling.SampleGen.Tests;

[TestClass]
public partial class ToolkitSampleMetadataTests
{
    [TestMethod]
    public void SampleAttributeOnUnsupportedType()
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{
                [ToolkitSample(id: nameof(Sample), ""Test Sample"", description: """")]
                public partial class Sample
                {{
                }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SampleAttributeOnUnsupportedType.Id, DiagnosticDescriptors.SampleNotReferencedInMarkdown.Id);
    }

    [TestMethod]
    public void SampleOptionPaneAttributeOnUnsupportedType()
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{
                [ToolkitSampleOptionsPane(sampleId: nameof(Sample))]
                public partial class SampleOptionsPane
                {{
                }}

                [ToolkitSample(id: nameof(Sample), ""Test Sample"", description: """")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {{
                }}
            }}

            namespace Windows.UI.Xaml.Controls
            {{
                public class UserControl {{ }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SampleOptionPaneAttributeOnUnsupportedType.Id, DiagnosticDescriptors.SampleNotReferencedInMarkdown.Id);
    }

    [TestMethod]
    public void SampleAttributeValid()
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{

                [ToolkitSample(id: nameof(Sample), ""Test Sample"", description: """")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {{
                }}
            }}

            namespace Windows.UI.Xaml.Controls
            {{
                public class UserControl {{ }}
            }}";

        // TODO: We should have this return the references to the registries or something so we can check the generated output?
        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SampleNotReferencedInMarkdown.Id);
    }
}
