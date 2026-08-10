// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Tests.Verifiers;

public static class CSharpSourceGeneratorVerifier<TGenerator>
    where TGenerator : ISourceGenerator, new()
{
    public static void Verify(
        string source,
        string expectedDiagnosticId,
        DiagnosticSeverity expectedSeverity,
        string expectedMessage,
        string expectedLocationText)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ZeroSerializerAttribute).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new TGenerator();
        var driver = CSharpGeneratorDriver.Create(ImmutableArray.Create<ISourceGenerator>(generator));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var targetDiagnostics = diagnostics.Where(d => d.Id == expectedDiagnosticId).ToList();
        Assert.Single(targetDiagnostics);

        var diagnostic = targetDiagnostics[0];
        Assert.Equal(expectedSeverity, diagnostic.Severity);
        Assert.Contains(expectedMessage, diagnostic.GetMessage());

        if (expectedLocationText != null)
        {
            var lineSpan = diagnostic.Location.GetLineSpan();
            Assert.True(lineSpan.IsValid);
            var locationText = source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
            Assert.Equal(expectedLocationText, locationText);
        }
    }

    public static void VerifyNoDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ZeroSerializerAttribute).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new TGenerator();
        var driver = CSharpGeneratorDriver.Create(ImmutableArray.Create<ISourceGenerator>(generator));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        Assert.Empty(diagnostics.Where(d => d.Id.StartsWith("ZEROS")));
    }
}
