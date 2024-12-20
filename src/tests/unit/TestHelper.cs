using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

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
    public static (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput<T>(string source, bool throwCompilerErrors = true)
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

        var diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Any(o => o.Severity == DiagnosticSeverity.Error && !o.GetMessage().Contains("does not contain a definition for 'OptionsToString'")))
        {
            if (throwCompilerErrors)
                throw new Exception(diagnostics.Select(o => o.ToString()).Aggregate((a, b) => a + "\n" + b));
            return (diagnostics, string.Empty);
        }
        var originalTreeCount = compilation.SyntaxTrees.Length;

        CSharpGeneratorDriver
            .Create(new T())
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out diagnostics);
        var output = string.Join("\n", outputCompilation.SyntaxTrees.Skip(originalTreeCount).Select(t => t.ToString()));

        return (diagnostics, output);
    }

    public static Task Verify<T>(string source, Action<ImmutableArray<Diagnostic>>? assertDiag = null, bool throwCompilerErrors = true ) where T : IIncrementalGenerator, new()
    {
        var (diag, output) = GetGeneratedOutput<T>(source, throwCompilerErrors);
        if (assertDiag != null)
        {
            assertDiag(diag);
            if (diag.Any(o => o.Severity == DiagnosticSeverity.Error))
            {
                return Task.CompletedTask;
            }
        }
        else
        {
            Assert.Empty(diag);
        }
        return Verifier.Verify(output).UseDirectory("Snapshots");
    }

    public static Task VerifyFile<T>(string filename, Action<ImmutableArray<Diagnostic>>? assertDiag = null, bool throwCompilerErrors = true) where T : IIncrementalGenerator, new()
    {
        var source = File.ReadAllText(filename);
        var (diag, output) = GetGeneratedOutput<T>(source, throwCompilerErrors);
        if (assertDiag != null)
        {
            assertDiag(diag);
        }
        else
        {
            Assert.Empty(diag);
        }
        return Verifier.Verify(output).UseDirectory("Snapshots").UseParameters(Path.GetFileNameWithoutExtension(filename));

    }

}