using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
namespace Seekatar.OptionToStringGenerator;

public class ClassToGenerate : ItemToGenerate
{
    public ClassToGenerate(INamedTypeSymbol classSymbol, List<IPropertySymbol> values) : base(classSymbol, values)
    {
        ClassSymbol = classSymbol;
    }

    public INamedTypeSymbol ClassSymbol {  get; }

    public override AttributeData? GetFormat() => ClassSymbol.GetAttributes().Where(a => a.AttributeClass?.ContainingNamespace?.ToString() == "Seekatar.OptionToStringGenerator").FirstOrDefault();

}

[Generator]
public class OptionToStringGenerator : OptionGeneratorBase<ClassDeclarationSyntax,ClassToGenerate>
{
    public const string FullAttributeName = "Seekatar.OptionToStringGenerator.OptionsToStringAttribute";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Do a simple filter for classes
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),        // get classes with an attribute, must be fast
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // get a class with the my attribute
            .Where(static m => m is not null)!; // filter out attributed classes that we don't care about

        // Combine the selected classes with the `Compilation`
        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
            = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate the source using the compilation and classes
        context.RegisterSourceOutput(compilationAndClasses,
            (spc, source) => Execute("ClassExtensions", source.Item1, source.Item2, spc));
    }
    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;

    static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // we know the node is a ClassDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        return HasAttribute(context, FullAttributeName, classDeclarationSyntax.AttributeLists) ? classDeclarationSyntax : null;

    }

    protected override List<ClassToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classSyntax, CancellationToken ct, SourceProductionContext context)
    {
        var classToGenerate = new List<ClassToGenerate>();

        // Get the semantic representation of our marker attribute
        INamedTypeSymbol? classAttribute = compilation.GetTypeByMetadataName(FullAttributeName);

        if (classAttribute == null)
        {
            // If this is null, the compilation couldn't find the marker attribute type
            // which suggests there's something very wrong! Bail out..
            return classToGenerate;
        }

        foreach (ClassDeclarationSyntax classDeclarationSyntax in classSyntax)
        {
            // stop if we're asked to
            ct.ThrowIfCancellationRequested();

            // Get the semantic representation of the class syntax
            SemanticModel semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
            {
                // something went wrong, bail out
                continue;
            }

            // <TEST>
            //var propertyDeclarations = classDeclarationSyntax
            //    .DescendantNodes()
            //    .OfType<PropertyDeclarationSyntax>();

            //foreach (var propertyDeclaration in propertyDeclarations)
            //{
            //    // Get the symbol for the property
            //    var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration) as IPropertySymbol;
            //    // Get the type of the property
            //    var propertyType = propertySymbol.Type;
            //    // Get all interfaces implemented by the property's type
            //    var allInterfaces = propertyType.AllInterfaces;
            //    if (allInterfaces.Any())
            //    {
            //        Debug.WriteLine($"got it for {propertySymbol.Name}");
            //    } else
            //    {
            //        Debug.WriteLine($"nope for {propertySymbol.Name}"); // Get Here for IList<> other properties that aren't generic work.'
            //    }
            //}
            // </TEST>

            var namedArguments = classSymbol.GetAttributes()
                    .Where(a => a.AttributeClass?.ContainingNamespace?.ToString() == "Seekatar.OptionToStringGenerator").FirstOrDefault()?
                    .NamedArguments;
            var excludeParent = namedArguments?.Any(n => n.Key == nameof(OptionsToStringAttribute.ExcludeParents)
                                             && n.Value.Value is not null
                                             && (bool)n.Value.Value);
            var sort = namedArguments?.Any(n => n.Key == nameof(OptionsToStringAttribute.Sort)
                                             && n.Value.Value is not null
                                             && (bool)n.Value.Value);
            var members = GetAllPublicProperties(classSymbol, excludeParent, sort);

            // Create an ClassToGenerate for use in the generation phase
            classToGenerate.Add(new ClassToGenerate(classSymbol, members));
        }

        return classToGenerate;
    }

    protected override ImmutableArray<AttributeData> AttributesForMember(IPropertySymbol symbol, ClassToGenerate propertyToGenerate)
    {
        return symbol.GetAttributes();
    }
}
