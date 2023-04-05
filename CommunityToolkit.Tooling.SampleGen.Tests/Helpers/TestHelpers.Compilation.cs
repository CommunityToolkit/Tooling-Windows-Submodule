// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CommunityToolkit.Tooling.SampleGen.Tests.Helpers;

public static partial class TestHelpers
{
    internal static IEnumerable<MetadataReference> GetAllReferencedAssemblies()
    {
        return from assembly in AppDomain.CurrentDomain.GetAssemblies()
               where !assembly.IsDynamic
               let reference = MetadataReference.CreateFromFile(assembly.Location)
               select reference;
    }

    internal static SyntaxTree ToSyntaxTree(this string source)
    {
        return CSharpSyntaxTree.ParseText(source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
    }

    internal static CSharpCompilation CreateCompilation(this SyntaxTree syntaxTree, string assemblyName, IEnumerable<MetadataReference>? references = null)
    {
        return CSharpCompilation.Create(assemblyName, new[] { syntaxTree }, references ?? GetAllReferencedAssemblies(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    internal static CSharpCompilation CreateCompilation(this IEnumerable<SyntaxTree> syntaxTree, string assemblyName, IEnumerable<MetadataReference>? references = null)
    {
        return CSharpCompilation.Create(assemblyName, syntaxTree, references ?? GetAllReferencedAssemblies(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    internal static GeneratorDriver CreateSourceGeneratorDriver(this SyntaxTree syntaxTree, IIncrementalGenerator generator)
    {
        return CSharpGeneratorDriver.Create(generator).WithUpdatedParseOptions((CSharpParseOptions)syntaxTree.Options);
    }

    internal static GeneratorDriver CreateSourceGeneratorDriver(this SyntaxTree syntaxTree, params IIncrementalGenerator[] generators)
    {
        return CSharpGeneratorDriver.Create(generators).WithUpdatedParseOptions((CSharpParseOptions)syntaxTree.Options);
    }

    internal static GeneratorDriver WithMarkdown(this GeneratorDriver driver, params string[] markdownFilesToCreate)
    {
        foreach (var markdown in markdownFilesToCreate)
        {
            if (!string.IsNullOrWhiteSpace(markdown))
            {
                var text = new InMemoryAdditionalText(@"C:\pathtorepo\components\experiment\samples\experiment.Samples\documentation.md", markdown);
                driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(text));
            }
        }

        return driver;
    }
}
