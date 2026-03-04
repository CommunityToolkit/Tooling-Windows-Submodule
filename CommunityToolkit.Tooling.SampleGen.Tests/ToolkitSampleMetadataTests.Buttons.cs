// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Tooling.SampleGen.Diagnostics;
using CommunityToolkit.Tooling.SampleGen.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace CommunityToolkit.Tooling.SampleGen.Tests;

public partial class ToolkitSampleMetadataTests
{
    [TestMethod]
    public void SampleButtonAttributeOnNonSample()
    {
        var source = """
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {
                    [ToolkitSampleButton(Title = "Click Me")]
                    private void OnButtonClick()
                    {
                    }
                }
            }

            namespace Windows.UI.Xaml.Controls
            {
                public class UserControl { }
            }
            """;

        var result = source.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME);

        result.AssertDiagnosticsAre(DiagnosticDescriptors.SampleButtonAttributeOnNonSample);
        result.AssertNoCompilationErrors();
    }

    [TestMethod]
    public void SampleButtonAttributeValid()
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
                    [ToolkitSampleButton(Title = "Click Me")]
                    private void OnButtonClick()
                    {
                    }
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

    [TestMethod]
    public void SampleButtonAttributeMultipleButtons()
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
                    [ToolkitSampleButton(Title = "Add Item")]
                    private void AddItemClick()
                    {
                    }

                    [ToolkitSampleButton(Title = "Clear Items")]
                    private void ClearItemsClick()
                    {
                    }
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

    [TestMethod]
    public void SampleButtonCommand_GeneratedRegistryExecutesMethod()
    {
        // The sample registry is designed to be declared in the sample project,
        // and generated in the project head. To test end-to-end execution of the
        // generated button commands, we replicate this setup, verify the generated
        // registry source, then execute the same command against a matching instance.
        var sampleSource = """
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {
                [ToolkitSample(id: nameof(Sample), "Test Sample", description: "")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {
                    public int Counter { get; set; }

                    public Sample()
                    {
                    }

                    [ToolkitSampleButton(Title = "Increment")]
                    private void IncrementCounter()
                    {
                        Counter++;
                    }
                }
            }

            namespace Windows.UI.Xaml.Controls
            {
                public class UserControl { }
            }
            """;

        // Compile sample project as a metadata reference for the generator
        var sampleProjectAssembly = sampleSource.ToSyntaxTree()
            .CreateCompilation("MyApp.Samples")
            .ToMetadataReference();

        // Create application head that references the sample project
        var headCompilation = string.Empty
            .ToSyntaxTree()
            .CreateCompilation("MyApp.Head")
            .AddReferences(sampleProjectAssembly);

        // Run source generator to produce the registry
        var result = headCompilation.RunSourceGenerator<ToolkitSampleMetadataGenerator>();

        result.AssertDiagnosticsAre();
        result.AssertNoCompilationErrors();

        // Verify the generated registry contains the expected button command
        var registrySource = result.Compilation.GetFileContentsByName("ToolkitSampleRegistry.g.cs");
        StringAssert.Contains(registrySource, @"new CommunityToolkit.Tooling.SampleGen.Metadata.ToolkitSampleButtonCommand(""Increment"", ""IncrementCounter"")");

        // Now verify the generated command mechanism works end-to-end
        // by creating the same command the registry would and binding it to an instance
        var testInstance = new SampleButtonCommandTestTarget();
        Assert.AreEqual(0, testInstance.Counter);

        var button = new Metadata.ToolkitSampleButtonCommand("Increment", "IncrementCounter");
        button.BindToInstance(testInstance);

        Assert.AreEqual("Increment", button.Title);
        Assert.AreEqual("IncrementCounter", button.MethodName);
        Assert.IsTrue(button.CanExecute(null!));

        // Execute and verify the counter was incremented
        button.Execute(null!);
        Assert.AreEqual(1, testInstance.Counter);

        button.Execute(null!);
        Assert.AreEqual(2, testInstance.Counter);
    }

    [TestMethod]
    public void SampleButtonCommand_AssemblyAttributeBridge()
    {
        // Verifies that the sample project emits assembly-level ToolkitSampleButtonDataAttribute
        // and that the head project can read them to populate button commands in the registry.
        // This tests the mechanism that bridges private method visibility across PE references.
        var sampleSource = """
            using System.ComponentModel;
            using CommunityToolkit.Tooling.SampleGen;
            using CommunityToolkit.Tooling.SampleGen.Attributes;

            namespace MyApp
            {
                [ToolkitSample(id: nameof(Sample), "Test Sample", description: "")]
                public partial class Sample : Windows.UI.Xaml.Controls.UserControl
                {
                    [ToolkitSampleButton(Title = "Increment")]
                    private void IncrementCounter()
                    {
                    }
                }
            }

            namespace Windows.UI.Xaml.Controls
            {
                public class UserControl { }
            }
            """;

        // Step 1: Run the generator on the sample project to produce assembly-level button attributes
        var sampleResult = sampleSource.RunSourceGenerator<ToolkitSampleMetadataGenerator>(SAMPLE_ASM_NAME);
        sampleResult.AssertNoCompilationErrors();

        // Verify the sample project generated the assembly-level button metadata
        var buttonMetadataSource = sampleResult.Compilation.GetFileContentsByName("ToolkitSampleButtonMetadata.g.cs");
        StringAssert.Contains(buttonMetadataSource, @"ToolkitSampleButtonDataAttribute(""MyApp.Sample"", ""IncrementCounter"", ""Increment"")");

        // Step 2: Compile the sample project WITH the generated source and create a reference
        var sampleWithGenerated = sampleResult.Compilation.ToMetadataReference();

        // Step 3: Run the generator on the head project
        var headCompilation = string.Empty
            .ToSyntaxTree()
            .CreateCompilation("MyApp.Head")
            .AddReferences(sampleWithGenerated);

        var headResult = headCompilation.RunSourceGenerator<ToolkitSampleMetadataGenerator>();

        headResult.AssertDiagnosticsAre();
        headResult.AssertNoCompilationErrors();

        // Verify the head project's registry includes the button command
        var registrySource = headResult.Compilation.GetFileContentsByName("ToolkitSampleRegistry.g.cs");
        StringAssert.Contains(registrySource, @"new CommunityToolkit.Tooling.SampleGen.Metadata.ToolkitSampleButtonCommand(""Increment"", ""IncrementCounter"")");
    }
}

/// <summary>
/// Mirrors the generated sample class structure for testing <see cref="Metadata.ToolkitSampleButtonCommand"/> execution.
/// </summary>
internal class SampleButtonCommandTestTarget
{
    public int Counter { get; set; }

    private void IncrementCounter() => Counter++;
}
