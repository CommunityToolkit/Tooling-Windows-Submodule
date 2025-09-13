// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace CommunityToolkit.Tooling.SampleGen.Tests.Helpers;

public static partial class TestHelpers
{
    internal static IEnumerable<MetadataReference> GetAllReferencedAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));
    }

    internal static SyntaxTree ToSyntaxTree(this string source)
    {
        return CSharpSyntaxTree.ParseText(source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12));
    }

    internal static CSharpCompilation CreateCompilation(this SyntaxTree syntaxTree, string assemblyName, IEnumerable<MetadataReference>? references = null)
    {
        return CSharpCompilation.Create(assemblyName, [syntaxTree], references ?? GetAllReferencedAssemblies(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    internal static CSharpCompilation CreateCompilation(this IEnumerable<SyntaxTree> syntaxTree, string assemblyName, IEnumerable<MetadataReference>? references = null)
    {
        return CSharpCompilation.Create(assemblyName, syntaxTree, references ?? GetAllReferencedAssemblies(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    internal static GeneratorDriver CreateSourceGeneratorDriver(this SyntaxTree syntaxTree, params IIncrementalGenerator[] generators)
    {
        return CSharpGeneratorDriver.Create(generators).WithUpdatedParseOptions((CSharpParseOptions)syntaxTree.Options);
    }

    internal static GeneratorDriver CreateSourceGeneratorDriver(this Compilation compilation, params IIncrementalGenerator[] generators)
    {
        return CSharpGeneratorDriver.Create(generators).WithUpdatedParseOptions((CSharpParseOptions)compilation.SyntaxTrees.First().Options);
    }

    internal static GeneratorDriver WithMarkdown(this GeneratorDriver driver, params string[] markdownFilesToCreate)
    {
        foreach (var markdown in markdownFilesToCreate)
        {
            if (!string.IsNullOrWhiteSpace(markdown))
            {
                var text = new InMemoryAdditionalText(@"C:\pathtorepo\components\experiment\samples\documentation.md", markdown);
                driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(text));
            }
        }

        return driver;
    }

    internal static GeneratorDriver WithCsproj(this GeneratorDriver driver, params string[] csprojFilesToCreate)
    {
        foreach (var proj in csprojFilesToCreate)
        {
            if (!string.IsNullOrWhiteSpace(proj))
            {
                var text = new InMemoryAdditionalText(@"C:\pathtorepo\components\experiment\src\componentname.csproj", proj);
                driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(text));
            }
        }

        return driver;
    }
}
