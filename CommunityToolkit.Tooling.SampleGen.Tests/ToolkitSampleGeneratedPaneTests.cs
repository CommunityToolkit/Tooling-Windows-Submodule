// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Tooling.SampleGen.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommunityToolkit.Tooling.SampleGen.Tests;

[TestClass]
public partial class ToolkitSampleGeneratedPaneTests
{
    [TestMethod]
    public void PaneOption_GeneratesWithoutDiagnostics()
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{
                [ToolkitSampleBoolOption(""Test"", false, Title = ""Toggle y"")]
                [ToolkitSampleMultiChoiceOption(""TextFontFamily"", ""Segoe UI"", ""Arial"", ""Consolas"", Title = ""Font family"")]

                [ToolkitSample(id: nameof(Sample), ""Test Sample"", description: """")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {{
                    public Sample()
                    {{
                        var x = this.Test;
                        var y = this.TextFontFamily;
                    }}
                }}
            }}

            namespace Windows.UI.Xaml.Controls
            {{
                public class UserControl {{ }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleOptionGenerator>(source, string.Empty);
    }

    [TestMethod]
    public void PaneOption_GeneratesTitleProperty()
    {
        var source = """
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {
                [ToolkitSampleNumericOption("TextSize", 12, 8, 48, 2, false, Title = "FontSize")]
                [ToolkitSample(id: nameof(Sample), "Test Sample", description: "")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {
                    public Sample()
                    {
                        var x = this.Test;
                        var y = this.TextFontFamily;
                    }
                }
            }

            namespace Windows.UI.Xaml.Controls
            {
                public class UserControl { }
            }
        """;

        var result = """
            #nullable enable
            namespace CommunityToolkit.Tooling.SampleGen;

            public static class ToolkitSampleRegistry
            {
                public static System.Collections.Generic.Dictionary<string, CommunityToolkit.Tooling.SampleGen.Metadata.ToolkitSampleMetadata> Listing
                { get; } = new() {
                    ["Sample"] = new CommunityToolkit.Tooling.SampleGen.Metadata.ToolkitSampleMetadata("Sample", "Test Sample", "", typeof(MyApp.Sample), () => new MyApp.Sample(), null, null, new CommunityToolkit.Tooling.SampleGen.Metadata.IGeneratedToolkitSampleOptionViewModel[] { new CommunityToolkit.Tooling.SampleGen.Metadata.ToolkitSampleNumericOptionMetadataViewModel(name: "TextSize", initial: 12, min: 8, max: 48, step: 2, showAsNumberBox: false, title: "FontSize") })
                };
            }
            """;

        ToolkitSampleMetadataTests.VerifyGenerateSources("MyApp.Tests", source, new[] { new ToolkitSampleMetadataGenerator() }, ignoreDiagnostics: true, ("ToolkitSampleRegistry.g.cs", result));
    }

    // https://github.com/CommunityToolkit/Labs-Windows/issues/175
    [TestMethod]
    public void PaneOption_GeneratesProperty_DuplicatePropNamesAcrossSampleClasses()
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{
                [ToolkitSampleBoolOption(""Test"", false, Title = ""Toggle y"")]
                [ToolkitSampleMultiChoiceOption(""TextFontFamily"", ""Segoe UI"", ""Arial"", ""Consolas"", Title = ""Font family"")]

                [ToolkitSample(id: nameof(Sample), ""Test Sample"", description: """")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {{
                    public Sample()
                    {{
                        var x = this.Test;
                        var y = this.TextFontFamily;
                    }}
                }}

                [ToolkitSampleBoolOption(""Test"", false, Title = ""Toggle y"")]
                [ToolkitSampleMultiChoiceOption(""TextFontFamily"", ""Segoe UI"", ""Arial"", ""Consolas"", Title = ""Font family"")]

                [ToolkitSample(id: nameof(Sample2), ""Test Sample"", description: """")]
                public partial class Sample2 : Windows.UI.Xaml.Controls.UserControl
                {{
                    public Sample2()
                    {{
                        var x = this.Test;
                        var y = this.TextFontFamily;
                    }}
                }}
            }}

            namespace Windows.UI.Xaml.Controls
            {{
                public class UserControl {{ }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleOptionGenerator>(source, string.Empty);
    }

    [TestMethod]
    public void PaneOptionOnNonSample()
    {
        string source = @"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {
                [ToolkitSampleBoolOption(""BindToMe"", false, Title =  ""Toggle visibility"")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {
                }
            }

            namespace Windows.UI.Xaml.Controls
            {
                public class UserControl { }
            }";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SamplePaneOptionAttributeOnNonSample.Id);
    }

    [DataRow("", DisplayName = "Empty string"), DataRow(" ", DisplayName = "Only whitespace"), DataRow("Test ", DisplayName = "Text with whitespace")]
    [DataRow("_", DisplayName = "Underscore"), DataRow("$", DisplayName = "Dollar sign"), DataRow("%", DisplayName = "Percent symbol")]
    [DataRow("class", DisplayName = "Reserved keyword 'class'"), DataRow("string", DisplayName = "Reserved keyword 'string'"), DataRow("sealed", DisplayName = "Reserved keyword 'sealed'"), DataRow("ref", DisplayName = "Reserved keyword 'ref'")]
    [TestMethod]
    public void PaneOptionWithBadName(string name)
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{
                [ToolkitSample(id: nameof(Sample), ""Test Sample"", description: """")]
                [ToolkitSampleBoolOption(""{name}"", false, Title =  ""Toggle visibility"")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {{
                }}
            }}

            namespace Windows.UI.Xaml.Controls
            {{
                public class UserControl {{ }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SamplePaneOptionWithBadName.Id, DiagnosticDescriptors.SampleNotReferencedInMarkdown.Id);
    }


    [TestMethod]
    public void PaneOptionWithConflictingPropertyName()
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{
                [ToolkitSampleBoolOption(""IsVisible"", false, Title =  ""Toggle x"")]
                [ToolkitSample(id: nameof(Sample), ""Test Sample"", description: """")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {{
                    public string IsVisible {{ get; set; }}
                }}
            }}

            namespace Windows.UI.Xaml.Controls
            {{
                public class UserControl {{ }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SamplePaneOptionWithConflictingName.Id, DiagnosticDescriptors.SampleNotReferencedInMarkdown.Id);
    }

    [TestMethod]
    public void PaneOptionWithConflictingInheritedPropertyName()
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{
                [ToolkitSampleBoolOption(""IsVisible"", false, Title =  ""Toggle x"")]
                [ToolkitSample(id: nameof(Sample), ""Test Sample"", description: """")]
                public partial class Sample : Base
                {{
                }}

                public class Base : Windows.UI.Xaml.Controls.UserControl
                {{
                    public string IsVisible {{ get; set; }}
                }}
            }}

            namespace Windows.UI.Xaml.Controls
            {{
                public class UserControl {{ }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SamplePaneOptionWithConflictingName.Id, DiagnosticDescriptors.SampleNotReferencedInMarkdown.Id);
    }

    [TestMethod]
    public void PaneOptionWithDuplicateName()
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{
                [ToolkitSampleBoolOption(""test"", false, Title =  ""Toggle x"")]
                [ToolkitSampleBoolOption(""test"", false, Title =  ""Toggle y"")]
                [ToolkitSampleMultiChoiceOption(""TextFontFamily"", ""Segoe UI"", ""Arial"", Title = ""Text foreground"")]

                [ToolkitSample(id: nameof(Sample), ""Test Sample"", description: """")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {{
                }}
            }}

            namespace Windows.UI.Xaml.Controls
            {{
                public class UserControl {{ }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SamplePaneOptionWithDuplicateName.Id, DiagnosticDescriptors.SampleNotReferencedInMarkdown.Id);
    }

    [TestMethod]
    public void PaneOptionWithDuplicateName_AllowedBetweenSamples()
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{
                [ToolkitSampleBoolOption(""test"", false, Title =  ""Toggle y"")]

                [ToolkitSample(id: nameof(Sample), ""Test Sample"", description: """")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {{
                }}

                [ToolkitSampleBoolOption(""test"", false, Title =  ""Toggle y"")]

                [ToolkitSample(id: nameof(Sample2), ""Test Sample"", description: """")]
                public partial class Sample2 : Windows.UI.Xaml.Controls.UserControl
                {{
                }}
            }}

            namespace Windows.UI.Xaml.Controls
            {{
                public class UserControl {{ }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SampleNotReferencedInMarkdown.Id);
    }

    [TestMethod]
    public void SampleGeneratedOptionAttributeOnUnsupportedType()
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{
                [ToolkitSampleMultiChoiceOption(""TextFontFamily"", ""Segoe UI"", ""Arial"", ""Consolas"", Title = ""Font family"")]
                [ToolkitSampleBoolOption(""Test"", false, Title =  ""Toggle visibility"")]
                public partial class Sample
                {{
                }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SampleGeneratedOptionAttributeOnUnsupportedType.Id, DiagnosticDescriptors.SamplePaneOptionAttributeOnNonSample.Id);
    }

    [TestMethod]
    public void PaneMultipleChoiceOptionWithNoChoices()
    {
        var source = $@"
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {{
                [ToolkitSampleMultiChoiceOption(""TextFontFamily"", Title = ""Font family"")]

                [ToolkitSample(id: nameof(Sample), ""Test Sample"", description: """")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {{
                }}
            }}

            namespace Windows.UI.Xaml.Controls
            {{
                public class UserControl {{ }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SamplePaneMultiChoiceOptionWithNoChoices.Id, DiagnosticDescriptors.SampleNotReferencedInMarkdown.Id);
    }

    [TestMethod]
    public void GeneratedPaneOption_ButtonAction()
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
                    [ToolkitSampleButtonAction(label: ""Raise notification"")]
                    private void RaiseNotification()
                    {{
                    }}
                }}
            }}

            namespace Windows.UI.Xaml.Controls
            {{
                public class UserControl {{ }}
            }}";

        TestHelpers.VerifyGeneratedDiagnostics<ToolkitSampleMetadataGenerator>(source, string.Empty, DiagnosticDescriptors.SampleNotReferencedInMarkdown.Id);
    }
}
