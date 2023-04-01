using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommunityToolkit.Tooling.SampleGen.Tests;

public static class TestHelpers
{
    internal static void VerifyGeneratedDiagnostics<TGenerator>(string source, string markdown, params string[] diagnosticsIds)
        where TGenerator : class, IIncrementalGenerator, new()
    {
        VerifyGeneratedDiagnostics<TGenerator>(CSharpSyntaxTree.ParseText(source), markdown, diagnosticsIds);
    }

    internal static void VerifyGeneratedDiagnostics<TGenerator>(SyntaxTree syntaxTree, string markdown, params string[] diagnosticsIds)
        where TGenerator : class, IIncrementalGenerator, new()
    {
        var references = GetAllReferencedAssemblies();
        var compilation = CreateCompilation("original.Samples", syntaxTree, references);
        var generator = new TGenerator();
        var driver = CreateGeneratorDriver(syntaxTree, generator);

        if (!string.IsNullOrWhiteSpace(markdown))
        {
            var text = new InMemoryAdditionalText(@"C:\pathtorepo\components\experiment\samples\experiment.Samples\documentation.md", markdown);
            driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(text));
        }

        _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation generatorCompilation, out ImmutableArray<Diagnostic> postGeneratorCompilationDiagnostics);

        VerifyCompilationErrors(compilation);
        VerifyCompilationErrors(generatorCompilation);
        VerifyDiagnostics(diagnosticsIds, postGeneratorCompilationDiagnostics);
    }

    internal static void VerifyGenerateSources(string assemblyName, string source, IIncrementalGenerator[] generators, bool ignoreDiagnostics = false, params (string Filename, string Text)[] results)
    {
        var references = GetAllReferencedAssemblies();
        var syntaxTree = CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CreateCompilation(assemblyName, syntaxTree, references);
        var driver = CreateGeneratorDriver(syntaxTree, generators);

        _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation generatorCompilation, out ImmutableArray<Diagnostic> diagnostics);

        if (!ignoreDiagnostics)
        {
            CollectionAssert.AreEquivalent(Array.Empty<Diagnostic>(), diagnostics);
        }

        VerifyCompilationErrors(compilation);
        VerifyGeneratedSources(results, generatorCompilation);
    }

    internal static IEnumerable<MetadataReference> GetAllReferencedAssemblies()
    {
        return from assembly in AppDomain.CurrentDomain.GetAssemblies()
               where !assembly.IsDynamic
               let reference = MetadataReference.CreateFromFile(assembly.Location)
               select reference;
    }

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

    private static CSharpCompilation CreateCompilation(string assemblyName, SyntaxTree syntaxTree, IEnumerable<MetadataReference> references)
    {
        return CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static GeneratorDriver CreateGeneratorDriver(SyntaxTree syntaxTree, IIncrementalGenerator generator)
    {
        return CSharpGeneratorDriver
            .Create(generator).WithUpdatedParseOptions((CSharpParseOptions)syntaxTree.Options);
    }

    private static GeneratorDriver CreateGeneratorDriver(SyntaxTree syntaxTree, IIncrementalGenerator[] generators)
    {
        return CSharpGeneratorDriver.Create(generators).WithUpdatedParseOptions((CSharpParseOptions)syntaxTree.Options);
    }

    private static void VerifyDiagnostics(string[] diagnosticsIds, ImmutableArray<Diagnostic> diagnostics)
    {
        HashSet<string> resultingIds = diagnostics.Select(diagnostic => diagnostic.Id).ToHashSet();
        Assert.IsTrue(resultingIds.SetEquals(diagnosticsIds), $"Expected one of [{string.Join(", ", diagnosticsIds)}] diagnostic Ids. Got [{string.Join(", ", resultingIds)}]");
    }

    private static void VerifyCompilationErrors(Compilation outputCompilation)
    {
        var generatedCompilationDiaghostics = outputCompilation.GetDiagnostics();
        Assert.IsTrue(generatedCompilationDiaghostics.All(x => x.Severity != DiagnosticSeverity.Error), $"Expected no generated compilation errors. Got: \n{string.Join("\n", generatedCompilationDiaghostics.Where(x => x.Severity == DiagnosticSeverity.Error).Select(x => $"[{x.Id}: {x.GetMessage()}]"))}");
    }

    private static void VerifyGeneratedSources((string Filename, string Text)[] results, Compilation outputCompilation)
    {
        foreach ((string filename, string text) in results)
        {
            SyntaxTree generatedTree = outputCompilation.SyntaxTrees.Single(tree => Path.GetFileName(tree.FilePath) == filename);
            Assert.AreEqual(text, generatedTree.ToString());
        }
    }
}
