using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Seekatar.OptionToStringGenerator;

public readonly struct ClassToGenerate
{
    public readonly string Name;
    public readonly List<IPropertySymbol> Values;
    public readonly string Accessibility { get; }

    public ClassToGenerate(string name, List<IPropertySymbol> values, string accessibility)
    {
        Name = name;
        Values = values;
        Accessibility = accessibility;
    }
}

[Generator]
public class OptionToStringGenerator : IIncrementalGenerator
{
    public const string FullAttributeName = "Seekatar.OptionToStringGenerator.OptionsToStringAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
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
            static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }
    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;

    static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // we know the node is a ClassDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        // loop through all the attributes 
        foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                {
                    // weird, we couldn't get the symbol, ignore it
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                // Is the attribute my attribute?
                if (fullName == FullAttributeName)
                {
                    // return the class
                    return classDeclarationSyntax;
                }
            }
        }

        // we didn't find the attribute we were looking for
        return null;
    }

    static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        // I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
        IEnumerable<ClassDeclarationSyntax> distinctClasses = classes.Distinct();

        // Convert each ClassDeclarationSyntax to an ClassToGenerate
        List<ClassToGenerate> classesToGenerate = GetTypesToGenerate(compilation, distinctClasses, context.CancellationToken);

        // If there were errors in the ClassDeclarationSyntax, we won't create an
        // ClassToGenerate for it, so make sure we have something to generate
        if (classesToGenerate.Count > 0)
        {
            // generate the source code and add it to the output
            string result = GenerateExtensionClass(classesToGenerate, context);
            context.AddSource("ClassExtensions.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }

    static List<ClassToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classSyntax, CancellationToken ct)
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

            // Get the full type name of the class
            // or OuterClass<T>.Color if it was nested in a generic type (for example)
            string className = classSymbol.ToString();
            string accessibility = classSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();

            // Get all the members in the class
            ImmutableArray<ISymbol> classMembers = classSymbol.GetMembers();
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

            // Create an ClassToGenerate for use in the generation phase
            classToGenerate.Add(new ClassToGenerate(className, members, accessibility));
        }

        return classToGenerate;
    }

    public static string GenerateExtensionClass(List<ClassToGenerate> classesToGenerate, SourceProductionContext context)
    {
        var sb = new StringBuilder();
        sb.Append("""
                    #nullable enable
                    namespace Seekatar.OptionToStringGenerator
                    {
                        public static partial class ClassExtensions
                        {

                    """);
        foreach (var classToGenerate in classesToGenerate)
        {
            int maxLen = 0;
            foreach (var member in classToGenerate.Values)
            {
                if (member.Name.Length > maxLen)
                {
                    maxLen = member.Name.Length;
                }
            }

            // method signature
            sb.Append($"        {classToGenerate.Accessibility} static string OptionsToString(this ").Append(classToGenerate.Name).Append(
                      """"
                       o)
                              {
                                  return $"""

                      """");
            sb.Append("                    " + classToGenerate.Name).AppendLine(":");

            // each property
            string format = $"                      {{0,-{maxLen}}} : {{{{OptionsToStringAttribute.Format(o.";
            foreach (var member in classToGenerate.Values)
            {
                bool ignored = false;
                var formatParameters = "";
                var attributeCount = 0;
                for (int i = 0; i < member.GetAttributes().Length; i++)
                {
                    var attribute = member.GetAttributes()[i];
                    if (attribute.AttributeClass?.ContainingNamespace?.ToString() == "Seekatar.OptionToStringGenerator")
                    {
                        attributeCount++;
                        if (attribute.AttributeClass?.Name == "OutputIgnoreAttribute")
                            ignored = true;
                        else if (attribute.AttributeClass?.Name == "OutputMaskAttribute")
                            formatParameters += ",prefixLen:" + (attribute.NamedArguments.Length > 0 ? (attribute.NamedArguments[0].Value.Value?.ToString() ?? "0") : "0");
                        else if (attribute.AttributeClass?.Name == "OutputLengthOnlyAttribute")
                            formatParameters = ",lengthOnly:true";
                        else if (attribute.AttributeClass?.Name == "OutputRegexAttribute")
                        {
                            var regexOk = false;
                            foreach (var n in attribute.NamedArguments)
                            {
                                if (n.Key == "Regex" && n.Value.Value is not null)
                                {
                                    formatParameters += ",regex:\"" + n.Value.Value + "\"";
                                    regexOk = true;
                                }
                                else if (n.Key == "IgnoreCase" && n.Value.Value is not null)
                                {
                                    formatParameters += ",ignoreCase:" + n.Value.Value.ToString().ToLowerInvariant();
                                }
                            }
                            if (!regexOk)
                            {
                                var diag = Diagnostic.Create(new DiagnosticDescriptor(
                                                        id: "SEEK001",
                                                        title: "Missing regex parameter",
                                                        messageFormat: "You must specify a regex parameter",
                                                        category: "Usage",
                                                        defaultSeverity: DiagnosticSeverity.Error,
                                                        isEnabledByDefault: true
                                                     ), member.Locations[0]);
                                 context.ReportDiagnostic(diag);
                            }
                        }
                    }
                }

                if (attributeCount > 1)
                {
                    var diag = Diagnostic.Create(new DiagnosticDescriptor(
                        id: "SEEK002",
                        title: "Multiple format attributes",
                        messageFormat: "You can only use one formatting attribute on a property",
                        category: "Usage",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true
                        ), member.Locations[0]);
                    context.ReportDiagnostic(diag);
                }
                if (!ignored)
                    sb.AppendFormat(format, member.Name).Append(member.Name).Append(formatParameters).AppendLine(")}");
            }

            // end of method
            sb.Append(""""
                                          """;
                              }

                      """");

        }

        sb.Append(@"
    }
}");

        return sb.ToString();
    }
}
