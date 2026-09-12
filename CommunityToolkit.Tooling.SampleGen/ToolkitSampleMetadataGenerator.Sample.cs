// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Tooling.SampleGen.Attributes;
using CommunityToolkit.Tooling.SampleGen.Diagnostics;
using CommunityToolkit.Tooling.SampleGen.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;

namespace CommunityToolkit.Tooling.SampleGen;

/// <summary>
/// Crawls all referenced projects for <see cref="ToolkitSampleAttribute"/>s and generates a static method that returns metadata for each one found. Uses markdown files to generate a listing of <see cref="ToolkitFrontMatter"/> data and an index of references samples for them.
/// </summary>
[Generator]
public partial class ToolkitSampleMetadataGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var symbolsInExecutingAssembly = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 } or MethodDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol(ctx.Node))
            .Where(static m => m is not null)
            .Select(static (x, _) => x!);

        var symbolsInReferencedAssemblies = context.CompilationProvider
            .SelectMany((x, _) => x.SourceModule.ReferencedAssemblySymbols)
            .SelectMany((asm, _) => asm.GlobalNamespace.CrawlForAllSymbols());

        var markdownFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".md"))
            .Collect();

        var csprojFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".csproj"))
            .Collect();

        var assemblyName = context.CompilationProvider.Select((x, _) => x.Assembly.Name);

                // Read assembly-level button metadata from referenced assemblies.
                // Button attributes are placed on private methods in sample classes, which are not visible
                // through PE metadata references. To bridge this gap, the sample project's generator phase
                // emits assembly-level attributes that encode button data. The head project reads these.
                var assemblyButtonData = context.CompilationProvider
                    .Select((compilation, _) =>
                    {
                        var buttons = ImmutableArray.CreateBuilder<(string SampleTypeName, string MethodName, string Title)>();
                        foreach (var asm in compilation.SourceModule.ReferencedAssemblySymbols)
                        {
                            foreach (var attr in asm.GetAttributes())
                            {
                                if (attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                                    $"global::{typeof(ToolkitSampleButtonDataAttribute).FullName}" &&
                                    attr.ConstructorArguments.Length == 3)
                                {
                                    buttons.Add((
                                        (string)(attr.ConstructorArguments[0].Value ?? ""),
                                        (string)(attr.ConstructorArguments[1].Value ?? ""),
                                        (string)(attr.ConstructorArguments[2].Value ?? "")
                                    ));
                                }
                            }
                        }
                        return buttons.ToImmutable();
                    });

        // Only generate diagnostics (sample projects)
        // Skip creating the registry for symbols in the executing assembly. This would place an incomplete registry in each sample project and cause compiler errors.
        Execute(symbolsInExecutingAssembly, skipRegistry: true);

        // Only generate the registry (project head)
        // Skip diagnostics for symbols in referenced assemblies. We can't place diagnostics here even if we tried,
        // and running diagnostics here will cause errors when executing in the sample project, due to not finding sample metadata in the provided symbols.
        Execute(symbolsInReferencedAssemblies, skipDiagnostics: true);

        void Execute(IncrementalValuesProvider<ISymbol> types, bool skipDiagnostics = false, bool skipRegistry = false)
        {
            // Get all attributes + the original type symbol.
            var allAttributeData = types.SelectMany(static (sym, _) => sym.GetAttributes().Select(x => (sym, x)));

            // Find and reconstruct generated pane option attributes + the original type symbol.
            var generatedPaneOptions = allAttributeData
                .Select((x, _) =>
                {
                    (ISymbol Symbol, ToolkitSampleOptionBaseAttribute Attribute) item = default;

                    // Reconstruct declared sample option attribute class instances from Roslyn symbols.
                    if (x.Item2.TryReconstructAs<ToolkitSampleBoolOptionAttribute>() is ToolkitSampleBoolOptionAttribute boolOptionAttribute)
                    {
                        item = (x.Item1, boolOptionAttribute);
                    }
                    else if (x.Item2.TryReconstructAs<ToolkitSampleMultiChoiceOptionAttribute>() is ToolkitSampleMultiChoiceOptionAttribute multiChoiceOptionAttribute)
                    {
                        item = (x.Item1, multiChoiceOptionAttribute);
                    }
                    else if (x.Item2.TryReconstructAs<ToolkitSampleNumericOptionAttribute>() is ToolkitSampleNumericOptionAttribute numericOptionAttribute)
                    {
                        item = (x.Item1, numericOptionAttribute);
                    }
                    else if (x.Item2.TryReconstructAs<ToolkitSampleTextOptionAttribute>() is ToolkitSampleTextOptionAttribute textOptionAttribute)
                    {
                        item = (x.Item1, textOptionAttribute);
                    }

                    // Add extra property data, like Title, back to Attribute
                    if (item.Attribute is not null && x.Item2.TryGetNamedArgument(nameof(ToolkitSampleOptionBaseAttribute.Title), out string? title) && !string.IsNullOrWhiteSpace(title))
                    {
                        item.Attribute.Title = title;
                    }

                    return item;
                })
                .Collect();

            // Find and reconstruct sample attributes
            var toolkitSampleAttributeData = allAttributeData
                .Select(static (data, _) =>
                {
                    if (data.Item2.TryReconstructAs<ToolkitSampleAttribute>() is ToolkitSampleAttribute sampleAttribute)
                        return (Attribute: sampleAttribute, AttachedQualifiedTypeName: data.Item1.ToString(), Symbol: data.Item1);

                    return default;
                })
                .Where(static x => x != default)
                .Collect();

            var optionsPaneAttributes = allAttributeData
                .Select(static (x, _) => (x.Item2.TryReconstructAs<ToolkitSampleOptionsPaneAttribute>(), x.Item1))
                .Where(static x => x.Item1 is not null)
                .Collect();

            // Find and reconstruct button attributes from method symbols + the containing type symbol.
            var buttonAttributes = allAttributeData
                .Select((x, _) =>
                {
                    (ISymbol ContainingType, ISymbol Method, ToolkitSampleButtonAttribute Attribute) item = default;

                    if (x.Item1 is IMethodSymbol methodSymbol &&
                        x.Item2.TryReconstructAs<ToolkitSampleButtonAttribute>() is ToolkitSampleButtonAttribute buttonAttribute)
                    {
                        buttonAttribute.MethodName = methodSymbol.Name;

                        if (x.Item2.TryGetNamedArgument(nameof(ToolkitSampleButtonAttribute.Title), out string? title) && !string.IsNullOrWhiteSpace(title))
                        {
                            buttonAttribute.Title = title;
                        }

                        item = (methodSymbol.ContainingType, x.Item1, buttonAttribute);
                    }

                    return item;
                })
                .Where(static x => x.Attribute is not null)
                .Collect();

            var all = optionsPaneAttributes
                .Combine(toolkitSampleAttributeData)
                .Combine(generatedPaneOptions)
                .Combine(buttonAttributes)
                .Combine(assemblyButtonData)
                .Combine(markdownFiles)
                .Combine(csprojFiles)
                .Combine(assemblyName);

            // TODO: We can make this static if we could pass in our two boolean values as context, no idea how to do that...
            context.RegisterSourceOutput(all, (ctx, data) =>
            {
                var (((((((optionsPaneAttributes, toolkitSampleAttributes), generatedPaneOptions), buttonAttributes), assemblyButtonDataValues), markdownFiles), csprojFiles), currentAssembly) = data;

                var toolkitSampleAttributeData = toolkitSampleAttributes.Where(x => x != default).Distinct();
                var optionsPaneAttributeData = optionsPaneAttributes.Where(x => x != default).Distinct();
                var generatedOptionPropertyData = generatedPaneOptions.Where(x => x.Attribute is not null && x.Symbol is not null);
                var buttonAttributeData = buttonAttributes.Where(x => x.Attribute is not null && x.ContainingType is not null);

                // Assembly-level button metadata from referenced assemblies (bridges the PE visibility gap)
                var assemblyButtonsByTypeName = assemblyButtonDataValues
                    .GroupBy(x => x.SampleTypeName)
                    .ToDictionary(g => g.Key, g => g.Select(x => new ToolkitSampleButtonAttribute { MethodName = x.MethodName, Title = x.Title }));

                var markdownFileData = markdownFiles.Where(x => x != default).Distinct();
                var csprojFileData = csprojFiles.Where(x => x != default).Distinct();

                var markdownProjPairings = markdownFileData.Select<AdditionalText, (AdditionalText Document, AdditionalText? CsProj)>((docFile, _) =>
                {
                    // TODO: We use these splits a lot to extra path info, so we should probably make a helper function?
                    var rootPathFile = docFile.Path.Split(new string[] { @"\components\", "/components/", @"\tooling\", "/tooling/" }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Split(new char[] { '/', '\\' }).FirstOrDefault();

                    var csproj = csprojFileData.FirstOrDefault(csProjFile => csProjFile.Path.Split(new string[] { @"\components\", "/components/", @"\tooling\", "/tooling/" }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Split(new char[] { '/', '\\' }).FirstOrDefault() == rootPathFile);

                    return (docFile, csproj);
                });

                var isExecutingInSampleProject = currentAssembly?.EndsWith(".Samples") ?? false;

                // Reconstruct sample metadata from attributes
                var sampleMetadata = toolkitSampleAttributeData
                     .GroupBy(x => x.Attribute.Id).Select(x => x.First()) // Filter out non-unique Ids. Diagnostics happen below.
                     .ToDictionary(
                        sample => sample.Attribute.Id,
                        sample =>
                            new ToolkitSampleRecord(
                                sample.Attribute.Id,
                                sample.Attribute.DisplayName,
                                sample.Attribute.Description,
                                sample.AttachedQualifiedTypeName,
                                optionsPaneAttributeData.FirstOrDefault(x => x.Item1?.SampleId == sample.Attribute.Id).Item2?.ToString(),
                                generatedOptionPropertyData.Where(x => x.Symbol.Equals(sample.Symbol, SymbolEqualityComparer.Default)).Select(x => x.Item2),
                                buttonAttributeData.Where(x => x.ContainingType.Equals(sample.Symbol, SymbolEqualityComparer.Default)).Select(x => x.Attribute)
                                    .Concat(assemblyButtonsByTypeName.TryGetValue(sample.AttachedQualifiedTypeName, out var asmButtons) ? asmButtons : Enumerable.Empty<ToolkitSampleButtonAttribute>())
                            )
                    );

                var docFrontMatter = GatherDocumentFrontMatter(ctx, markdownProjPairings);

                if (isExecutingInSampleProject && !skipDiagnostics)
                {
                    ReportSampleDiagnostics(ctx, toolkitSampleAttributeData, optionsPaneAttributeData, generatedOptionPropertyData, buttonAttributeData);
                    ReportDocumentDiagnostics(ctx, sampleMetadata, markdownFileData, toolkitSampleAttributeData, docFrontMatter);

                    // Emit assembly-level attributes encoding button metadata so the head project
                    // can read them (private method attributes are invisible through PE references).
                    GenerateButtonMetadataSource(ctx, buttonAttributeData);
                }

                if (!isExecutingInSampleProject && !skipRegistry)
                {
                    CreateDocumentRegistry(ctx, docFrontMatter);
                    CreateSampleRegistry(ctx, sampleMetadata);

                    // These diagnostics need to scan all symbols referenced from a sample head.
                    ReportDiagnosticsForConflictingSampleId(ctx, toolkitSampleAttributeData);
                }
            });
        }
    }

    private static void CreateSampleRegistry(SourceProductionContext ctx, Dictionary<string, ToolkitSampleRecord> sampleMetadata)
    {
        // TODO: Emit a better error that no samples are here?
        if (sampleMetadata.Count == 0)
            return;

        var source = BuildRegistrationCallsFromMetadata(sampleMetadata);
        ctx.AddSource($"ToolkitSampleRegistry.g.cs", source);
    }

    private static void ReportSampleDiagnostics(SourceProductionContext ctx,
                                          IEnumerable<(ToolkitSampleAttribute Attribute, string AttachedQualifiedTypeName, ISymbol Symbol)> toolkitSampleAttributeData,
                                          IEnumerable<(ToolkitSampleOptionsPaneAttribute?, ISymbol)> optionsPaneAttribute,
                                          IEnumerable<(ISymbol, ToolkitSampleOptionBaseAttribute)> generatedOptionPropertyData,
                                          IEnumerable<(ISymbol ContainingType, ISymbol Method, ToolkitSampleButtonAttribute Attribute)> buttonAttributeData)
    {
        ReportDiagnosticsForInvalidAttributeUsage(ctx, toolkitSampleAttributeData, optionsPaneAttribute, generatedOptionPropertyData);
        ReportDiagnosticsForLinkedOptionsPane(ctx, toolkitSampleAttributeData, optionsPaneAttribute);
        ReportDiagnosticsGeneratedOptionsPane(ctx, toolkitSampleAttributeData, generatedOptionPropertyData);
        ReportDiagnosticsForButtonAttributes(ctx, toolkitSampleAttributeData, buttonAttributeData);
    }

    private static void ReportDiagnosticsForInvalidAttributeUsage(SourceProductionContext ctx,
                                                                  IEnumerable<(ToolkitSampleAttribute Attribute, string AttachedQualifiedTypeName, ISymbol Symbol)> toolkitSampleAttributeData,
                                                                  IEnumerable<(ToolkitSampleOptionsPaneAttribute?, ISymbol)> optionsPaneAttribute,
                                                                  IEnumerable<(ISymbol, ToolkitSampleOptionBaseAttribute)> generatedOptionPropertyData)
    {
        var toolkitAttributesOnUnsupportedType = toolkitSampleAttributeData.Where(x => x.Symbol is INamedTypeSymbol namedSym && !IsValidXamlControl(namedSym));
        var optionsAttributeOnUnsupportedType = optionsPaneAttribute.Where(x => x.Item2 is INamedTypeSymbol namedSym && !IsValidXamlControl(namedSym));
        var generatedOptionAttributeOnUnsupportedType = generatedOptionPropertyData.Where(x => x.Item1 is INamedTypeSymbol namedSym && !IsValidXamlControl(namedSym));


        foreach (var item in toolkitAttributesOnUnsupportedType)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SampleAttributeOnUnsupportedType, item.Symbol.Locations.FirstOrDefault()));


        foreach (var item in optionsAttributeOnUnsupportedType)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SampleOptionPaneAttributeOnUnsupportedType, item.Item2.Locations.FirstOrDefault()));


        foreach (var item in generatedOptionAttributeOnUnsupportedType)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SampleGeneratedOptionAttributeOnUnsupportedType, item.Item1.Locations.FirstOrDefault()));

    }

    private static void ReportDiagnosticsForConflictingSampleId(SourceProductionContext ctx,
                                                              IEnumerable<(ToolkitSampleAttribute Attribute, string AttachedQualifiedTypeName, ISymbol Symbol)> toolkitSampleAttributeData)
    {
        foreach (var sampleIdGroup in toolkitSampleAttributeData.GroupBy(x => x.Attribute.Id))
        {
            if (sampleIdGroup.Count() > 1)
                ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SampleIdAlreadyInUse, null, sampleIdGroup.Key));
        }
    }

    private static void ReportDiagnosticsForLinkedOptionsPane(SourceProductionContext ctx,
                                                              IEnumerable<(ToolkitSampleAttribute Attribute, string AttachedQualifiedTypeName, ISymbol Symbol)> toolkitSampleAttributeData,
                                                              IEnumerable<(ToolkitSampleOptionsPaneAttribute?, ISymbol)> optionsPaneAttribute)
    {
        // Check for options pane attributes with no matching sample ID
        var optionsPaneAttributeWithMissingOrInvalidSampleId = optionsPaneAttribute.Where(x => !toolkitSampleAttributeData.Any(sample => sample.Attribute.Id == x.Item1?.SampleId));

        foreach (var item in optionsPaneAttributeWithMissingOrInvalidSampleId)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.OptionsPaneAttributeWithMissingOrInvalidSampleId, item.Item2.Locations.FirstOrDefault()));
    }

    private static void ReportDiagnosticsGeneratedOptionsPane(SourceProductionContext ctx,
                                                              IEnumerable<(ToolkitSampleAttribute Attribute, string AttachedQualifiedTypeName, ISymbol Symbol)> toolkitSampleAttributeData,
                                                              IEnumerable<(ISymbol, ToolkitSampleOptionBaseAttribute)> generatedOptionPropertyData)
    {
        ReportGeneratedMultiChoiceOptionsPaneDiagnostics(ctx, generatedOptionPropertyData);

        // Check for generated options which don't have a valid sample attribute
        var generatedOptionsWithMissingSampleAttribute = generatedOptionPropertyData.Where(x => x.Item1 is INamedTypeSymbol && !toolkitSampleAttributeData.Any(sample => ReferenceEquals(sample.Symbol, x.Item1)));

        foreach (var item in generatedOptionsWithMissingSampleAttribute)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SamplePaneOptionAttributeOnNonSample, item.Item1.Locations.FirstOrDefault()));

        // Check for generated options with an empty or invalid name.
        var generatedOptionsWithBadName = generatedOptionPropertyData.Where(x => string.IsNullOrWhiteSpace(x.Item2.Name) || // Must not be null or empty
                                                                                !x.Item2.Name.Any(char.IsLetterOrDigit) || // Must be alphanumeric
                                                                                x.Item2.Name.Any(char.IsWhiteSpace) || // Must not have whitespace
                                                                                SyntaxFacts.GetKeywordKind(x.Item2.Name) != SyntaxKind.None); // Must not be a reserved keyword

        foreach (var item in generatedOptionsWithBadName)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SamplePaneOptionWithBadName, item.Item1.Locations.FirstOrDefault(), item.Item1.ToString()));

        // Check for generated options with duplicate names.
        var generatedOptionsWithDuplicateName = generatedOptionPropertyData.GroupBy(x => x.Item1, SymbolEqualityComparer.Default) // Group by containing symbol (allow reuse across samples)
                                                                           .SelectMany(y => y.GroupBy(x => x.Item2.Name) // In this symbol, group options by name.
                                                                                             .Where(x => x.Count() > 1)); // Options grouped by name should only contain 1 item.

        foreach (var item in generatedOptionsWithDuplicateName)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SamplePaneOptionWithDuplicateName, item.SelectMany(x => x.Item1.Locations).FirstOrDefault(), item.Key));

        // Check for generated options that conflict with an existing property name
        var generatedOptionsWithConflictingPropertyNames = generatedOptionPropertyData.Where(x => x.Item1 is INamedTypeSymbol sym && GetAllMembers(sym).Any(y => x.Item2.Name == y.Name));

        foreach (var item in generatedOptionsWithConflictingPropertyNames)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SamplePaneOptionWithConflictingName, item.Item1.Locations.FirstOrDefault(), item.Item2.Name));
    }

    private static void ReportGeneratedMultiChoiceOptionsPaneDiagnostics(SourceProductionContext ctx, IEnumerable<(ISymbol, ToolkitSampleOptionBaseAttribute)> generatedOptionPropertyData)
    {
        foreach (var item in generatedOptionPropertyData)
        {
            if (item.Item2 is ToolkitSampleMultiChoiceOptionAttribute multiChoiceAttr && multiChoiceAttr.Choices.Length == 0)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SamplePaneMultiChoiceOptionWithNoChoices, item.Item1.Locations.FirstOrDefault(), item.Item2.Title));
            }
        }
    }

    private static string BuildRegistrationCallsFromMetadata(IDictionary<string, ToolkitSampleRecord> sampleMetadata)
    {
        return $@"#nullable enable
namespace CommunityToolkit.Tooling.SampleGen;

public static class ToolkitSampleRegistry
{{
    public static System.Collections.Generic.Dictionary<string, {typeof(ToolkitSampleMetadata).FullName}> Listing
    {{ get; }} = new() {{
        {string.Join(",\n        ", sampleMetadata.Select(MetadataToRegistryCall).ToArray())}
    }};
}}";
    }

    private static string MetadataToRegistryCall(KeyValuePair<string, ToolkitSampleRecord> kvp)
    {
        var metadata = kvp.Value;
        var sampleControlTypeParam = $"typeof({metadata.SampleAssemblyQualifiedName})";
        var sampleControlFactoryParam = $"() => new {metadata.SampleAssemblyQualifiedName}()";
        var generatedSampleOptionsParam = $"new {typeof(IGeneratedToolkitSampleOptionViewModel).FullName}[] {{ {string.Join(", ", BuildNewGeneratedSampleOptionMetadataSource(metadata).ToArray())} }}";
        var sampleOptionsParam = metadata.SampleOptionsAssemblyQualifiedName is null ? "null" : $"typeof({metadata.SampleOptionsAssemblyQualifiedName})";
        var sampleOptionsPaneFactoryParam = metadata.SampleOptionsAssemblyQualifiedName is null ? "null" : $"x => new {metadata.SampleOptionsAssemblyQualifiedName}(({metadata.SampleAssemblyQualifiedName})x)";
        var sampleButtonsParam = metadata.GeneratedSampleButtons?.Any() == true
            ? $"new {typeof(ToolkitSampleButtonCommand).FullName}[] {{ {string.Join(", ", BuildNewGeneratedSampleButtonSource(metadata).ToArray())} }}"
            : "null";

        return @$"[""{kvp.Key}""] = new {typeof(ToolkitSampleMetadata).FullName}(""{metadata.Id}"", ""{metadata.DisplayName}"", ""{metadata.Description}"", {sampleControlTypeParam}, {sampleControlFactoryParam}, {sampleOptionsParam}, {sampleOptionsPaneFactoryParam}, {generatedSampleOptionsParam}, {sampleButtonsParam})";
    }

    private static IEnumerable<string> BuildNewGeneratedSampleOptionMetadataSource(ToolkitSampleRecord sample)
    {
        foreach (var item in sample.GeneratedSampleOptions ?? Enumerable.Empty<ToolkitSampleOptionBaseAttribute>())
        {
            if (item is ToolkitSampleMultiChoiceOptionAttribute multiChoiceAttr)
            {
                yield return $@"new {typeof(ToolkitSampleMultiChoiceOptionMetadataViewModel).FullName}(name: ""{multiChoiceAttr.Name}"", options: new[] {{ {string.Join(",", multiChoiceAttr.Choices.Select(x => $@"new {typeof(MultiChoiceOption).FullName}(""{x.Label}"", ""{x.Value}"")").ToArray())} }}, title: ""{multiChoiceAttr.Title}"")";
            }
            else if (item is ToolkitSampleBoolOptionAttribute boolAttribute)
            {
                yield return $@"new {typeof(ToolkitSampleBoolOptionMetadataViewModel).FullName}(name: ""{boolAttribute.Name}"", defaultState: {boolAttribute.DefaultState?.ToString().ToLower()}, title: ""{boolAttribute.Title}"")";
            }
            else if (item is ToolkitSampleNumericOptionAttribute numericAttribute)
            {
                yield return $@"new {typeof(ToolkitSampleNumericOptionMetadataViewModel).FullName}(name: ""{numericAttribute.Name}"", initial: {numericAttribute.Initial}, min: {numericAttribute.Min}, max: {numericAttribute.Max}, step: {numericAttribute.Step}, showAsNumberBox: {numericAttribute.ShowAsNumberBox.ToString().ToLower()}, title: ""{numericAttribute.Title}"")";
            }
            else if (item is ToolkitSampleTextOptionAttribute textAttribute)
            {
                yield return $@"new {typeof(ToolkitSampleTextOptionMetadataViewModel).FullName}(name: ""{textAttribute.Name}"", placeholderText: ""{textAttribute.PlaceholderText}"", title: ""{textAttribute.Title}"")";
            }
            else
            {
                throw new NotSupportedException($"Unsupported or unhandled type {item.GetType()}.");
            }
        }
    }

    private static IEnumerable<string> BuildNewGeneratedSampleButtonSource(ToolkitSampleRecord sample)
    {
        foreach (var button in sample.GeneratedSampleButtons ?? Enumerable.Empty<ToolkitSampleButtonAttribute>())
        {
            yield return $@"new {typeof(ToolkitSampleButtonCommand).FullName}(""{button.Title}"", ""{button.MethodName}"")";
        }
    }

    private static void ReportDiagnosticsForButtonAttributes(SourceProductionContext ctx,
                                                             IEnumerable<(ToolkitSampleAttribute Attribute, string AttachedQualifiedTypeName, ISymbol Symbol)> toolkitSampleAttributeData,
                                                             IEnumerable<(ISymbol ContainingType, ISymbol Method, ToolkitSampleButtonAttribute Attribute)> buttonAttributeData)
    {
        // Check for button attributes on methods where the containing class doesn't have ToolkitSampleAttribute
        var buttonsWithMissingSampleAttribute = buttonAttributeData.Where(x =>
            x.ContainingType is INamedTypeSymbol &&
            !toolkitSampleAttributeData.Any(sample => sample.Symbol.Equals(x.ContainingType, SymbolEqualityComparer.Default)));

        foreach (var item in buttonsWithMissingSampleAttribute)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SampleButtonAttributeOnNonSample, item.Method.Locations.FirstOrDefault(), item.ContainingType.ToString()));
    }

    /// <summary>
    /// Generates assembly-level attributes that encode button metadata discovered from
    /// method-level <see cref="ToolkitSampleButtonAttribute"/>s. These attributes are
    /// compiled into the sample assembly so the head project's generator can read them,
    /// since private method attributes are not visible through PE metadata references.
    /// </summary>
    private static void GenerateButtonMetadataSource(SourceProductionContext ctx,
                                                     IEnumerable<(ISymbol ContainingType, ISymbol Method, ToolkitSampleButtonAttribute Attribute)> buttonAttributeData)
    {
        var lines = buttonAttributeData
            .Select(b => $@"[assembly: global::{typeof(ToolkitSampleButtonDataAttribute).FullName}(""{b.ContainingType}"", ""{b.Attribute.MethodName}"", ""{b.Attribute.Title}"")]")
            .ToList();

        if (lines.Count > 0)
        {
            ctx.AddSource("ToolkitSampleButtonMetadata.g.cs",
                "// <auto-generated/>\n" + string.Join("\n", lines));
        }
    }

    /// <summary>
    /// Checks if a symbol is or inherits from a type representing a XAML framework.
    /// </summary>
    /// <returns><see langwork="true"/> if the <paramref name="symbol"/> is or inherits from a type representing a XAML framework.</returns>
    private static bool IsValidXamlControl(INamedTypeSymbol symbol)
    {
        // In UWP, Page inherits UserControl
        // In Uno, Page doesn't appear to inherit UserControl.
        var validSimpleTypeNames = new[] { "UserControl", "Page" };

        // UWP / Uno / WinUI 3 namespaces.
        var validNamespaceRoots = new[] { "Microsoft", "Windows" };

        // Recursively crawl the base types until either UserControl or Page is found.
        var validInheritedSymbol = symbol.CrawlBy(x => x?.BaseType, baseType => validNamespaceRoots.Any(x => $"{baseType}".StartsWith(x)) &&
                                                                                $"{baseType}".Contains(".UI.Xaml.Controls.") &&
                                                                                validSimpleTypeNames.Any(x => $"{baseType}".EndsWith(x)));

        var typeIsAccessible = symbol.DeclaredAccessibility == Accessibility.Public;

        return validInheritedSymbol != default && typeIsAccessible && !symbol.IsStatic;
    }

    private static IEnumerable<ISymbol> GetAllMembers(INamedTypeSymbol symbol)
    {
        foreach (var item in symbol.GetMembers())
            yield return item;

        if (symbol.BaseType is not null)
            foreach (var item in GetAllMembers(symbol.BaseType))
                yield return item;
    }
}
