using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Schema;

namespace Seekatar.OptionToStringGenerator;

public readonly struct ClassToGenerate
{
    public string Name => ClassSymbol.ToString();
    public readonly List<IPropertySymbol> Values;
    public Location Location => ClassSymbol.Locations[0];
    public readonly INamedTypeSymbol ClassSymbol { get; }
    public string Accessibility => ClassSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();

    public ClassToGenerate(INamedTypeSymbol classSymbol, List<IPropertySymbol> values)
    {
        ClassSymbol = classSymbol;
        Values = values;
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
            classToGenerate.Add(new ClassToGenerate(classSymbol, members));
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

            var classAttribute = classToGenerate.ClassSymbol.GetAttributes().Where(a => a.AttributeClass?.ContainingNamespace?.ToString() == "Seekatar.OptionToStringGenerator").FirstOrDefault();
            if (classAttribute is null) { continue; } // can't get here if it doesn't have the attribute, but just in case
            var indent = "  ";
            var separator = ":";
            var nameQuote = "";
            var openBrace = "";
            var closeBrace = "";
            var trailingComma = "";
            var openBrace2 = "{{";
            var closeBrace2 = "}";
            var leadDollar = "$";
            foreach (var n in classAttribute.NamedArguments)
            {
                if (n.Key == nameof(OptionsToStringAttribute.Json)
                    && n.Value.Value is not null
                    && (bool)n.Value.Value)
                {
                    separator = " :";
                    nameQuote = "\"";
                    openBrace = """
                                {
                                                    
                                """;
                    closeBrace = """
                                 }
                                                     
                                 """;
                    maxLen += 2;
                    trailingComma = ",";
                    openBrace2 = "{{{{";
                    closeBrace2 = "}}";
                    leadDollar = "$$";
                }
                else if (n.Key == nameof(OptionsToStringAttribute.Indent) && n.Value.Value is not null)
                {
                    indent = n.Value.Value.ToString();
                }
                else if (n.Key == nameof(OptionsToStringAttribute.Separator) && n.Value.Value is not null)
                {
                    separator = n.Value.Value.ToString();
                }
            }

            // method signature
            sb.Append($"        {classToGenerate.Accessibility} static string OptionsToString(this ").Append(classToGenerate.Name).Append(
                      $$""""
                       o)
                              {
                                  return {{leadDollar}}"""

                      """");

            sb.Append($"                    {openBrace}{nameQuote}{classToGenerate.Name}{nameQuote}{separator}{" "+openBrace.Trim()}").AppendLine();

            if (!classToGenerate.Values.Any())
            {
                var diag = Diagnostic.Create(new DiagnosticDescriptor(
                                        id: "SEEK003",
                                        title: "No properties found",
                                        messageFormat: "No public properties have an Output* attribute",
                                        category: "Usage",
                                        defaultSeverity: DiagnosticSeverity.Warning,
                                        isEnabledByDefault: true,
                                        helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek003-no-properties-found"
                                     ), classToGenerate.Location );
                context.ReportDiagnostic(diag);
                sb.AppendLine("                      No properties to display");
            }

            // each property
            string format = $"                    {indent}{{0,-{maxLen}}} {separator} {openBrace2}OptionsToStringAttribute.Format(o.";
            int j = 0;
            foreach (var member in classToGenerate.Values)
            {
                if (++j == classToGenerate.Values.Count)
                    trailingComma = "";

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
                            var message = "You must specify a regex parameter";
                            foreach (var n in attribute.NamedArguments)
                            {
                                if (n.Key == "Regex" && n.Value.Value is not null)
                                {
                                    var regex = n.Value.Value.ToString().Replace("\\", "\\\\");
                                    try
                                    {
                                        var r = new Regex(regex);
                                        formatParameters += ",regex:\"" + regex + "\"";
                                        regexOk = true;
                                    }
                                    catch (ArgumentException e)
                                    {
                                        message = "Bad regex: "+e.Message;
                                        break;
                                    }
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
                                                        title: "Missing or invalid regex parameter",
                                                        messageFormat: message,
                                                        category: "Usage",
                                                        defaultSeverity: DiagnosticSeverity.Error,
                                                        isEnabledByDefault: true,
                                                        helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek001-missing-or-invalid-regex-parameter"
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
                        isEnabledByDefault: true,
                        helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek002-multiple-format-attributes"
                        ), member.Locations[0]);
                    context.ReportDiagnostic(diag);
                }
                if (!ignored)
                    sb.AppendFormat(format, $"{nameQuote}{member.Name}{nameQuote}").Append(member.Name).Append(formatParameters).AppendLine($"){closeBrace2}{trailingComma}");
            }

            // end of method
            sb.Append($$""""
                                          {{closeBrace}}{{closeBrace}}""";
                              }

                      """");

        }

        sb.Append(@"
    }
}");

        return sb.ToString();
    }
}
