﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Seekatar.OptionToStringGenerator;

public readonly struct ClassToGenerate
{
    public readonly string Name;
    public readonly List<IPropertySymbol> Values;

    public ClassToGenerate(string name, List<IPropertySymbol> values)
    {
        Name = name;
        Values = values;
    }
}

[Generator]
public class OptionToStringGenerator : IIncrementalGenerator
{
    public const string FullAttributeName = "Seekatar.OptionToStringGenerator.OptionsToStringAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the marker attribute to the compilation
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ClassExtensionsAttribute.g.cs",
            SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));

        // Do a simple filter for classes
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s), // select classes with attributes
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // sect the class with the [ClassExtensions] attribute
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

        // loop through all the attributes on the method
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

                // Is the attribute the [ClassExtensions] attribute?
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
        // Create a list to hold our output
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

            // Get all the members in the class
            ImmutableArray<ISymbol> classMembers = classSymbol.GetMembers();
            var members = new List<IPropertySymbol>(classMembers.Length);

            // Get all the fields from the class, and add their name to the list
            foreach (ISymbol member in classMembers)
            {
                if (member is IPropertySymbol property && property.GetMethod is not null)
                {
                    members.Add(property);
                }
            }

            // Create an ClassToGenerate for use in the generation phase
            classToGenerate.Add(new ClassToGenerate(className, members));
        }

        return classToGenerate;
    }

    public static string GenerateExtensionClass(List<ClassToGenerate> classsToGenerate, SourceProductionContext context)
    {
        var sb = new StringBuilder();
        sb.Append("""
                    namespace Seekatar.ClassGenerators
                    {
                        public static partial class ClassExtensions
                        {
                            string Format(object o, bool lengthOnly = false, int prefixLen = -1, string? regex = null, bool ignoreCase = false)
                            {
                                if ( o is null ) return "<null>";

                                if (lengthOnly) return "Len = " + o.ToString().Length.ToString();

                                if (prefixLen >= 0) {
                                    var s = o.ToString();
                                    if (prefixLen < s.Length) {
                                        return "\"" + s.Substring(0, prefixLen) + new string('*', s.Length - prefixLen) + "\"";
                                    } else {
                                        return "\"" + s + "\"";
                                    }
                                } 

                                if (regex is not null) {
                                    var r = new System.Text.RegularExpressions.Regex(regex);
                                    var s = o.ToString();
                                    var m = r.Match(s);
                                    while (m.Success) {
                                        for ( int i = 1; i < m.Groups.Count; i++ ) {
                                            var cc = m.Groups[i].Captures;
                                            for ( int j = 0; j < cc.Count; j++ ) {
                                                s = s.Replace(cc[j].ToString(), "***");
                                            }
                                        }
                                        m = m.NextMatch();
                                    }
                                    return s;
                                } 

                                if (o is string) 
                                    return "\"" + o + "\"";
                                else 
                                    return o.ToString();
                            }

                    """);
        foreach (var classToGenerate in classsToGenerate)
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
            sb.Append("        public static string OptionsToString(this ").Append(classToGenerate.Name).Append(
                      """"
                       o)
                              {
                                  return $"""
                                          
                      """");
            sb.Append(classToGenerate.Name).AppendLine(":");

            // each property
            string format = $"                      {{0,-{maxLen}}} : {{{{Format(o.";
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
                            foreach (var n in attribute.NamedArguments)
                            {
                                if (n.Key == "Regex" && n.Value.Value is not null)
                                {
                                    formatParameters += ",regex:" + n.Value.Value;
                                }
                                else if (n.Key == "IgnoreCase" && n.Value.Value is not null)
                                {
                                    formatParameters += ",ignoreCase:" + n.Value.Value.ToString().ToLowerInvariant();
                                }
                            }
                        }
                    }
                }

                if (attributeCount > 1)
                {
                    var diag = Diagnostic.Create(new DiagnosticDescriptor(
                        id: "TEST01",
                        title: "Multiple format attributes",
                        messageFormat: "You can only use one formatting attribute on a property",
                        category: "Usage",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true
                        ), member.Locations[0]
                        );
                    context.ReportDiagnostic(diag);
                }
                else if (!ignored)
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
