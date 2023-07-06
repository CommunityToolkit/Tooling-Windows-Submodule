// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;

namespace CommunityToolkit.Tooling.SampleGen.Tests.Helpers;

public static partial class TestHelpers
{
    internal static SourceGeneratorRunResult RunSourceGenerator<TGenerator>(this string source, string assemblyName, string markdown = "") where TGenerator : class, IIncrementalGenerator, new() => RunSourceGenerator<TGenerator>(source.ToSyntaxTree(), assemblyName, markdown);

    internal static SourceGeneratorRunResult RunSourceGenerator<TGenerator>(this SyntaxTree syntaxTree, string assemblyName, string markdown = "")
        where TGenerator : class, IIncrementalGenerator, new()
    {
        var compilation = syntaxTree.CreateCompilation(assemblyName); // assembly name should always be supplied in param
        return RunSourceGenerator<TGenerator>(compilation, markdown);
    }

    internal static SourceGeneratorRunResult RunSourceGenerator<TGenerator>(this Compilation compilation, string markdown = "")
        where TGenerator : class, IIncrementalGenerator, new()
    {
        // Create a driver for the source generator
        var driver = compilation
            .CreateSourceGeneratorDriver(new TGenerator())
            .WithMarkdown(markdown);

        // Update the original compilation using the source generator
        _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation generatorCompilation, out ImmutableArray<Diagnostic> postGeneratorCompilationDiagnostics);

        return new(generatorCompilation, postGeneratorCompilationDiagnostics);
    }

    internal static void AssertDiagnosticsAre(this IEnumerable<Diagnostic> diagnostics, params DiagnosticDescriptor[] expectedDiagnosticDescriptors)
    {
        var expectedIds = expectedDiagnosticDescriptors.Select(x => x.Id).ToHashSet();
        var resultingIds = diagnostics.Select(diagnostic => diagnostic.Id).ToHashSet();

        Assert.IsTrue(resultingIds.SetEquals(expectedIds), $"Expected [{string.Join(", ", expectedIds)}] diagnostic Ids. Got [{string.Join(", ", resultingIds)}]");
    }

    internal static void AssertNoCompilationErrors(this Compilation outputCompilation)
    {
        var generatedCompilationDiagnostics = outputCompilation.GetDiagnostics();
        Assert.IsTrue(generatedCompilationDiagnostics.All(x => x.Severity != DiagnosticSeverity.Error), $"Expected no generated compilation errors. Got: \n{string.Join("\n", generatedCompilationDiagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).Select(x => $"[{x.Id}: {x.GetMessage()}]"))}");
    }

    internal static string GetFileContentsByName(this Compilation compilation, string filename)
    {
        var generatedTree = compilation.SyntaxTrees.SingleOrDefault(tree => Path.GetFileName(tree.FilePath) == filename);
        Assert.IsNotNull(generatedTree, $"No file named {filename} was generated");

        return generatedTree.ToString();
    }

    internal static void AssertSourceGenerated(this Compilation compilation, string filename, string expectedContents)
    {
    }

    internal static void AssertDiagnosticsAre(this SourceGeneratorRunResult result, params DiagnosticDescriptor[] expectedDiagnosticDescriptors) => AssertDiagnosticsAre(result.Diagnostics, expectedDiagnosticDescriptors);

    internal static void AssertNoCompilationErrors(this SourceGeneratorRunResult result) => AssertNoCompilationErrors(result.Compilation);
}
