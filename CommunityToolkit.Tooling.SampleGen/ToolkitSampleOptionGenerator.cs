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
                static (s, _) => s is ClassDeclarationSyntax c && c.AttributeLists.Count > 0 || s is MethodDeclarationSyntax m && m.AttributeLists.Count > 0,
                static (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol(ctx.Node))
            .Where(static m => m is not null)
            .Select(static (x, _) => x!);

        // Get all attributes + the original type symbol.
        var allAttributeData = classes.SelectMany((sym, _) => sym.GetAttributes().Select(x => (sym, x)));

        // Find and reconstruct attributes.
        var sampleAttributeOptions = allAttributeData
            .Select((x, _) =>
            {
                if (x.Item2.TryReconstructAs<ToolkitSampleBoolOptionAttribute>() is ToolkitSampleBoolOptionAttribute boolOptionAttribute)
                    return (Attribute: (ToolkitSampleOptionBaseAttribute)boolOptionAttribute, AttachedSymbol: x.Item1, Type: typeof(ToolkitSampleBoolOptionMetadataViewModel));

                if (x.Item2.TryReconstructAs<ToolkitSampleButtonActionAttribute>() is ToolkitSampleButtonActionAttribute buttonActionAttribute)
                    return (Attribute: (ToolkitSampleOptionBaseAttribute)buttonActionAttribute, AttachedSymbol: x.Item1, Type: typeof(ToolkitSampleButtonActionMetadataViewModel));

                if (x.Item2.TryReconstructAs<ToolkitSampleMultiChoiceOptionAttribute>() is ToolkitSampleMultiChoiceOptionAttribute multiChoiceOptionAttribute)
                    return (Attribute: (ToolkitSampleOptionBaseAttribute)multiChoiceOptionAttribute, AttachedSymbol: x.Item1, Type: typeof(ToolkitSampleMultiChoiceOptionMetadataViewModel));

                if (x.Item2.TryReconstructAs<ToolkitSampleNumericOptionAttribute>() is ToolkitSampleNumericOptionAttribute numericOptionAttribute)
                    return (Attribute: (ToolkitSampleOptionBaseAttribute)numericOptionAttribute, AttachedSymbol: x.Item1, Type: typeof(ToolkitSampleNumericOptionMetadataViewModel));

                if (x.Item2.TryReconstructAs<ToolkitSampleTextOptionAttribute>() is ToolkitSampleTextOptionAttribute textOptionAttribute)
                    return (Attribute: (ToolkitSampleOptionBaseAttribute)textOptionAttribute, AttachedSymbol: x.Item1, Type: typeof(ToolkitSampleTextOptionMetadataViewModel));

                return default;
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
                    SymbolKind.Method => BuildCommandProperty(attachedMethodSymbol: data.AttachedSymbol, data.Type),
                    SymbolKind.NamedType => BuildProperty(containingClassSymbol: data.AttachedSymbol, data.Attribute.Name, data.Attribute.TypeName, data.Type),
                    _ => throw new NotSupportedException("Only methods and classes are supported here."),
                };

                ctx.AddSource(name, dependencyPropertySource);
            }
        });

    }

    private static string BuildINotifyPropertyChangedImplementation(ISymbol attachedSymbol)
    {
        return $@"#nullable enable
using System.ComponentModel;

namespace {attachedSymbol.ContainingNamespace}
{{
    public partial class {attachedSymbol.Name} : {nameof(System.ComponentModel.INotifyPropertyChanged)}
    {{
		public event PropertyChangedEventHandler? PropertyChanged;
    }}
}}
";
    }

    private static string BuildGeneratedPropertyMetadataContainer(ISymbol attachedSymbol)
    {
        return $@"#nullable enable
using System.ComponentModel;
using System.Collections.Generic;

namespace {attachedSymbol.ContainingNamespace}
{{
    public partial class {attachedSymbol.Name} : {typeof(IToolkitSampleGeneratedOptionPropertyContainer).Namespace}.{nameof(IToolkitSampleGeneratedOptionPropertyContainer)}
    {{
        private IEnumerable<{typeof(IGeneratedToolkitSampleOptionViewModel).FullName}>? _generatedPropertyMetadata;

        public IEnumerable<{typeof(IGeneratedToolkitSampleOptionViewModel).FullName}>? GeneratedPropertyMetadata
        {{
            get => _generatedPropertyMetadata;
            set
            {{
                if (!(_generatedPropertyMetadata is null))
                {{
                    foreach (var item in _generatedPropertyMetadata)
                        item.PropertyChanged -= OnPropertyChanged;
                }}
                
                if (!(value is null))
                {{
                    foreach (var item in value)
                        item.PropertyChanged += OnPropertyChanged;
                }}               

                _generatedPropertyMetadata = value;
            }}
        }}

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);
    }}
}}
";
    }

    private static string BuildProperty(ISymbol containingClassSymbol, string propertyName, string typeName, Type viewModelType)
    {
        return $@"#nullable enable
using System.ComponentModel;
using System.Linq;

namespace {containingClassSymbol.ContainingNamespace}
{{
    public partial class {containingClassSymbol.Name}
    {{
        public {typeName} {propertyName}
        {{
            get => (({typeName})(({viewModelType.FullName})GeneratedPropertyMetadata!.First(x => x.Name == ""{propertyName}""))!.Value!)!;
            set
            {{
				if (GeneratedPropertyMetadata?.FirstOrDefault(x => x.Name == nameof({propertyName})) is {viewModelType.FullName} metadata)
				{{
                    metadata.Value = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof({propertyName})));
				}}
            }}
        }}
    }}
}}
";
    }

    private static string BuildCommandProperty(ISymbol attachedMethodSymbol, Type viewModelType)
    {
        // User-supplied command methods are instance methods, and must be called from the same containing object instance.
        // To generate a command property that points to this, we lazy-set the default command value in
        // the getter and pass the instance method in as the command delegate.

        return $$"""
#nullable enable
using System.ComponentModel;
using System.Linq;

namespace {{attachedMethodSymbol.ContainingNamespace}}
{
    public partial class {{attachedMethodSymbol.ContainingSymbol.Name}}
    {
        public {{typeof(System.Windows.Input.ICommand).FullName}} {{attachedMethodSymbol.Name}}Command
        {
            get
            {
                var metadata = GeneratedPropertyMetadata!.First(x => x.Name == nameof({{attachedMethodSymbol.Name}}));
                return ({{typeof(System.Windows.Input.ICommand).FullName}})(metadata.Value ??= new {{typeof(ToolkitSampleButtonCommand).FullName}}(() => {{attachedMethodSymbol.Name}}()));
            }
            set
            {
				if (GeneratedPropertyMetadata?.FirstOrDefault(x => x.Name == nameof({{attachedMethodSymbol.Name}})) is {{viewModelType.FullName}} metadata)
				{
                    metadata.Value = (object)value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof({{attachedMethodSymbol.Name}}Command)));
				}
            }
        }
    }
}
""";
    }
}
