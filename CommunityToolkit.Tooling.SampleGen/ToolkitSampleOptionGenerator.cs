// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Tooling.SampleGen.Attributes;
using CommunityToolkit.Tooling.SampleGen.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommunityToolkit.Tooling.SampleGen;

/// <summary>
/// For the generated sample pane options, this generator creates the backing properties needed for binding in the UI,
/// as well as implementing the <see cref="IToolkitSampleGeneratedOptionPropertyContainer"/> for relaying data between the options pane and the generated property.
/// </summary>
[Generator]
public class ToolkitSampleOptionGenerator : IIncrementalGenerator
{
    private readonly HashSet<string> _handledPropertyNames = new();
    private readonly HashSet<ISymbol> _handledContainingClasses = new(SymbolEqualityComparer.Default);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 } or MethodDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol(ctx.Node))
            .Where(static m => m is not null)
            .Select(static (x, _) => x!);

        // Get all attributes + the original type symbol.
        var allAttributeData = classes.SelectMany((sym, _) => sym.GetAttributes().Select(x => (Symbol: sym, AttributeData: x)));

        // Find and reconstruct attributes.
        var sampleAttributeOptions = allAttributeData
            .Select((x, _) =>
            {
                (ToolkitSampleOptionBaseAttribute Attribute, ISymbol AttachedSymbol, Type Type) item = default;

                if (x.AttributeData.AttributeClass?.ContainingNamespace.ToDisplayString() == typeof(ToolkitSampleEnumOptionAttribute<>).Namespace
                    && x.AttributeData.AttributeClass?.MetadataName == typeof(ToolkitSampleEnumOptionAttribute<>).Name)
                {
                    if (x.AttributeData.AttributeClass.TypeArguments.FirstOrDefault() is { } typeSymbol)
                    {
                        var parameters = x.AttributeData.ConstructorArguments.Select(GeneratorExtensions.PrepareParameterTypeForActivator).ToList();
                        parameters.Add(typeSymbol.ToDisplayString());
                        parameters.Add(Array.Empty<MultiChoiceOption>());
                        var multiChoiceOptionAttribute = (ToolkitSampleMultiChoiceOptionAttribute)Activator.CreateInstance(
                            typeof(ToolkitSampleMultiChoiceOptionAttribute), BindingFlags.NonPublic | BindingFlags.Instance,
                            null, parameters.ToArray(), null);
                            item = (multiChoiceOptionAttribute, x.Symbol, typeof(ToolkitSampleMultiChoiceOptionMetadataViewModel));
                    }
                }
                else if (x.AttributeData.TryReconstructAs<ToolkitSampleBoolOptionAttribute>() is { } boolOptionAttribute)
                {
                    item = (boolOptionAttribute, x.Symbol, typeof(ToolkitSampleBoolOptionMetadataViewModel));
                }
                else if (x.AttributeData.TryReconstructAs<ToolkitSampleMultiChoiceOptionAttribute>() is { } multiChoiceOptionAttribute)
                {
                    item = (multiChoiceOptionAttribute, x.Symbol, typeof(ToolkitSampleMultiChoiceOptionMetadataViewModel));
                }
                else if (x.AttributeData.TryReconstructAs<ToolkitSampleNumericOptionAttribute>() is { } numericOptionAttribute)
                {
                    item = (numericOptionAttribute, x.Symbol, typeof(ToolkitSampleNumericOptionMetadataViewModel));
                }
                else if (x.AttributeData.TryReconstructAs<ToolkitSampleTextOptionAttribute>() is { } textOptionAttribute)
                {
                    item = (textOptionAttribute, x.Symbol, typeof(ToolkitSampleTextOptionMetadataViewModel));
                }

                return item;
            })
            .Where(x => x != default);

        context.RegisterSourceOutput(sampleAttributeOptions, (ctx, data) =>
        {
            var format = new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                    memberOptions: SymbolDisplayMemberOptions.IncludeContainingType
                );

            var containingClass = data.AttachedSymbol.Kind switch
            {
                SymbolKind.Method => data.AttachedSymbol.ContainingSymbol,
                SymbolKind.NamedType => data.AttachedSymbol,
                _ => throw new NotSupportedException("Only methods and classes are supported here."),
            };

            var name = data.AttachedSymbol.Kind switch
            {
                SymbolKind.Method => $"{data.AttachedSymbol.ToDisplayString(format)}.CommandProperty.g",
                SymbolKind.NamedType => $"{data.AttachedSymbol.ToDisplayString(format)}.Property.{data.Attribute.Name}.g",
                _ => throw new NotSupportedException("Only methods and classes are supported here."),
            };

            // Generate property container and INPC
            if (this._handledContainingClasses.Add(containingClass))
            {
                if (containingClass is ITypeSymbol typeSym && !typeSym.AllInterfaces.Any(x => x.HasFullyQualifiedName("global::System.ComponentModel.INotifyPropertyChanged")))
                {
                    var inpcImpl = BuildINotifyPropertyChangedImplementation(containingClass);
                    ctx.AddSource($"{containingClass}.NotifyPropertyChanged.g", inpcImpl);
                }

                ctx.AddSource($"{containingClass.ToDisplayString(format)}.GeneratedPropertyContainer.g", BuildGeneratedPropertyMetadataContainer(containingClass));
            }

            // Generate property
            if (this._handledPropertyNames.Add(name))
            {
                var dependencyPropertySource = data.AttachedSymbol.Kind switch
                {
                    SymbolKind.NamedType => BuildProperty(containingClassSymbol: data.AttachedSymbol, data.Attribute.Name, data.Attribute.TypeName, data.Type),
                    _ => throw new NotSupportedException("Only methods and classes are supported here."),
                };

                ctx.AddSource(name, dependencyPropertySource);
            }
        });
    }

    private static string BuildINotifyPropertyChangedImplementation(ISymbol attachedSymbol)
    {
        return $$"""
                #nullable enable
                using System.ComponentModel;

                namespace {{attachedSymbol.ContainingNamespace}}
                {
                    public partial class {{attachedSymbol.Name}} : {{nameof(INotifyPropertyChanged)}}
                    {
                        public event PropertyChangedEventHandler? PropertyChanged;
                    }
                }

                """;
    }

    private static string BuildGeneratedPropertyMetadataContainer(ISymbol attachedSymbol)
    {
        return $$"""
                #nullable enable
                using System.ComponentModel;
                using System.Collections.Generic;

                namespace {{attachedSymbol.ContainingNamespace}}
                {
                    public partial class {{attachedSymbol.Name}} : {{typeof(IToolkitSampleGeneratedOptionPropertyContainer).Namespace}}.{{nameof(IToolkitSampleGeneratedOptionPropertyContainer)}}
                    {
                        private {{typeof(IGeneratedToolkitSampleOptionViewModel).FullName}}[]? _generatedPropertyMetadata;

                        public {{typeof(IGeneratedToolkitSampleOptionViewModel).FullName}}[]? GeneratedPropertyMetadata
                        {
                            get => _generatedPropertyMetadata;
                            set
                            {
                                if (_generatedPropertyMetadata is not null)
                                {
                                    foreach (var item in _generatedPropertyMetadata)
                                        item.PropertyChanged -= OnPropertyChanged;
                                }
                                 
                                if (value is not null)
                                {
                                    foreach (var item in value)
                                        item.PropertyChanged += OnPropertyChanged;
                                }

                                _generatedPropertyMetadata = value;
                            }
                        }

                        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);
                    }
                }

                """;
    }

    private static string BuildProperty(ISymbol containingClassSymbol, string propertyName, string typeName, Type viewModelType)
    {
        return $$"""
                #nullable enable
                using System.ComponentModel;
                using System.Linq;

                namespace {{containingClassSymbol.ContainingNamespace}}
                {
                    public partial class {{containingClassSymbol.Name}}
                    {
                        public {{typeName}} {{propertyName}}
                        {
                            get => ({{typeName}})(({{viewModelType.FullName}})GeneratedPropertyMetadata!.First(x => x.Name is "{{propertyName}}"))!.Value!;
                            set
                            {
                 			    if (GeneratedPropertyMetadata?.FirstOrDefault(x => x.Name is "{{propertyName}}") is {{viewModelType.FullName}} metadata)
                 			    {
                                    metadata.Value = value;
                                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("{{propertyName}}"));
                 			    }
                            }
                        }
                    }
                }

                """;
    }
}
