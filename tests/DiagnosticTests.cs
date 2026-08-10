// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using ZeroSerializer.Generator;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Tests;

public class DiagnosticTests
{
    [Fact]
    public void TestGenericTypeDiagnostic()
    {
        string source = @"
using ZeroSerializer;

[ZeroSerializer]
public class MyGenericClass<T>
{
    public int Value { get; set; }
}
";

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

        var generator = new ZeroSerializerGenerator();
        var driver = CSharpGeneratorDriver.Create(ImmutableArray.Create<ISourceGenerator>(generator));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // Check that ZEROS008 was reported!
        var genericDiagnostics = diagnostics.Where(d => d.Id == "ZEROS008").ToList();
        Assert.Single(genericDiagnostics);

        var diagnostic = genericDiagnostics[0];
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("MyGenericClass", diagnostic.GetMessage());
        Assert.Equal("Don't allow generic type for ZeroSerializer", diagnostic.Descriptor.Title);

        // Ensure the location is on the identifier
        var lineSpan = diagnostic.Location.GetLineSpan();
        Assert.True(lineSpan.IsValid);
        var locationText = source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
        Assert.Equal("MyGenericClass", locationText);
    }

    [Fact]
    public void TestNonGenericTypeNoDiagnostic()
    {
        string source = @"
using ZeroSerializer;

[ZeroSerializer]
public class MyNonGenericClass
{
    public int Value { get; set; }
}
";

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

        var generator = new ZeroSerializerGenerator();
        var driver = CSharpGeneratorDriver.Create(ImmutableArray.Create<ISourceGenerator>(generator));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // Check that ZEROS008 was NOT reported!
        var genericDiagnostics = diagnostics.Where(d => d.Id == "ZEROS008").ToList();
        Assert.Empty(genericDiagnostics);
    }
}
