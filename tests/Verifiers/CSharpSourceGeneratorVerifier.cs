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
        string sourceWithMarkup,
        string expectedDiagnosticId,
        DiagnosticSeverity expectedSeverity,
        string expectedMessage)
    {
        var (source, expectedStart, expectedLength) = ParseMarkup(sourceWithMarkup);

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

        if (expectedStart != -1)
        {
            Assert.Equal(expectedStart, diagnostic.Location.SourceSpan.Start);
            Assert.Equal(expectedLength, diagnostic.Location.SourceSpan.Length);
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

    private static (string cleanedSource, int start, int length) ParseMarkup(string sourceWithMarkup)
    {
        int startTagIndex = sourceWithMarkup.IndexOf("[|");
        if (startTagIndex == -1)
        {
            return (sourceWithMarkup, -1, -1);
        }

        int endTagIndex = sourceWithMarkup.IndexOf("|]", startTagIndex);
        if (endTagIndex == -1)
        {
            throw new ArgumentException("Markup start tag '[|' found but end tag '|]' is missing.");
        }

        string cleanedSource = sourceWithMarkup.Substring(0, startTagIndex) +
                               sourceWithMarkup.Substring(startTagIndex + 2, endTagIndex - startTagIndex - 2) +
                               sourceWithMarkup.Substring(endTagIndex + 2);

        int start = startTagIndex;
        int length = endTagIndex - startTagIndex - 2;

        return (cleanedSource, start, length);
    }
}
