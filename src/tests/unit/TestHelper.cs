using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using VerifyXunit;

namespace Seekatar.OptionToStringGenerator.Tests;

public static class ModuleInitializer
{
	[ModuleInitializer]
	public static void Init()
	{
		VerifySourceGenerators.Initialize();
	}
}

public static class TestHelper
{
    public static (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput<T>(string source)
        where T : IIncrementalGenerator, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
            .Select(_ => MetadataReference.CreateFromFile(_.Location))
            .Concat(new[] { MetadataReference.CreateFromFile(typeof(T).Assembly.Location) });
        var compilation = CSharpCompilation.Create(
            "generator",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var originalTreeCount = compilation.SyntaxTrees.Length;

        CSharpGeneratorDriver
            .Create(new T())
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        var output = string.Join("\n", outputCompilation.SyntaxTrees.Skip(originalTreeCount).Select(t => t.ToString()));

        return (diagnostics, output);
    }

    public static Task Verify(string source, Action<ImmutableArray<Diagnostic>>? assertDiag = null)
    {
        var (diag, output) = GetGeneratedOutput<OptionToStringGenerator>(source);
        if (assertDiag != null)
        {
            assertDiag(diag);
        }
        else
        {
            Assert.Empty(diag);
        }
        return Verifier.Verify(output).UseDirectory("Snapshots");

    }
}