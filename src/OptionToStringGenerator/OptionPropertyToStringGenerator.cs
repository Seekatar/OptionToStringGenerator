using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

using static Seekatar.OptionToStringGenerator.DiagnosticTemplates.Ids;
namespace Seekatar.OptionToStringGenerator;


public class PropertyToGenerate :  ItemToGenerate
{
    public Dictionary<string, AttributeData> Attributes { get; }
    public IPropertySymbol PropertySymbol { get; }
    public override AttributeData? GetFormat() => Attributes.FirstOrDefault(a => a.Value.AttributeClass?.Name == nameof(OutputPropertyFormatAttribute)).Value;
    public PropertyToGenerate(IPropertySymbol propertySymbol, Dictionary<string, AttributeData> attrs, List<IPropertySymbol> values) : base(propertySymbol, values)
    {
        PropertySymbol = propertySymbol;
        Attributes = attrs;
        var propertyType = PropertySymbol.Type;
        if (propertyType is INamedTypeSymbol typeSymbol)
        {
            Name = (typeSymbol.ContainingNamespace.IsGlobalNamespace ? "global::" : typeSymbol.ContainingNamespace.Name + ".") + typeSymbol.Name;
            Accessibility = typeSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();
        }
    }
}

[Generator]
public class OptionPropertyToStringGenerator : OptionGeneratorBase<PropertyDeclarationSyntax, PropertyToGenerate> 
{
    public const string FullAttributeName = "Seekatar.OptionToStringGenerator.OutputPropertyMaskAttribute";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Do a simple filter for properties
        IncrementalValuesProvider<PropertyDeclarationSyntax> propertyDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),        // get properties with an attribute, must be fast
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // get a property with the my attribute
            .Where(static m => m is not null)!; // filter out attributed properties that we don't care about

        // Combine the selected properties with the `Compilation`
        IncrementalValueProvider<(Compilation, ImmutableArray<PropertyDeclarationSyntax>)> compilationAndProperties
            = context.CompilationProvider.Combine(propertyDeclarations.Collect());

        // Generate the source using the compilation and properties
        context.RegisterSourceOutput(compilationAndProperties,
            (spc, source) => Execute("PropertyExtensions", source.Item1, source.Item2, spc));
    }
    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    => node is PropertyDeclarationSyntax m && m.AttributeLists.Count > 0;

    static PropertyDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // we know the node is a PropertyDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        var propertyDeclarationSyntax = (PropertyDeclarationSyntax)context.Node;

        return HasAttribute(context, FullAttributeName, propertyDeclarationSyntax.AttributeLists) ? propertyDeclarationSyntax : null;
    }

    protected override List<PropertyToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<PropertyDeclarationSyntax> propertySyntax, CancellationToken ct, SourceProductionContext context)
    {
        var propertyToGenerate = new List<PropertyToGenerate>();

        // Get the semantic representation of our marker attribute
        INamedTypeSymbol? propertyAttribute = compilation.GetTypeByMetadataName(FullAttributeName);


        if (propertyAttribute == null)
        {
            // If this is null, the compilation couldn't find the marker attribute type
            // which suggests there's something very wrong! Bail out..
            return propertyToGenerate;
        }

        foreach (PropertyDeclarationSyntax propertyDeclarationSyntax in propertySyntax)
        {
            // stop if we're asked to
            ct.ThrowIfCancellationRequested();

            // Get the semantic representation of the property syntax
            SemanticModel semanticModel = compilation.GetSemanticModel(propertyDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(propertyDeclarationSyntax) is not IPropertySymbol propertySymbol)
            {
                // something went wrong, bail out
                continue;
            }
            var propertyType = propertySymbol.Type;
            if (propertyType is not INamedTypeSymbol typeSymbol)
            {
                continue;
            }

            // get all the attributes on the property
            var attrs = propertySymbol.GetAttributes().Where(a => a.AttributeClass?.ContainingNamespace?.ToString() == "Seekatar.OptionToStringGenerator");
            attrs = attrs.Where(a => a.AttributeClass?.Name.StartsWith("OutputProperty") ?? false);
            var attrDict = new Dictionary<string, AttributeData>();
            foreach ( var a in attrs)
            {
                var name = a.NamedArguments.FirstOrDefault(o => o.Key == nameof(IPropertyAttribute.Name)).Value.Value?.ToString();
                if (name is null)
                {
                    context.Report(SEEK007, a.AttributeClass?.Locations[0], propertySymbol.Name);
                }
                else
                {
                    attrDict.Add(name, a);
                }
            }

            if (propertyType.TypeKind is not TypeKind.Class or TypeKind.Interface or TypeKind.Struct)
            {
                context.Report(SEEK008, propertySymbol.Locations[0], propertySymbol.Name, propertyType.Name);
                continue; // int is struct, string is class
            }

            ImmutableArray<ISymbol> classMembers = typeSymbol.GetMembers();
            var members = new List<IPropertySymbol>(classMembers.Length);

            // Get all the public properties with a get method
            foreach (ISymbol member in classMembers)
            {
                if (member is IPropertySymbol property
                    && property.GetMethod is not null
                    && property.DeclaredAccessibility == Accessibility.Public)
                {
                    members.Add(property);
                }
            }

            propertyToGenerate.Add(new PropertyToGenerate(propertySymbol, attrDict, members));
        }

        return propertyToGenerate;
    }

    protected override ImmutableArray<AttributeData> AttributesForMember(IPropertySymbol symbol, PropertyToGenerate propertyToGenerate)
    {
        return propertyToGenerate.Attributes.Where(a => a.Key == symbol.Name).Select(a => a.Value).ToImmutableArray();
    }
}
