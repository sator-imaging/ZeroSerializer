// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;
using Xunit;
using ZeroSerializer.Generator;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Tests;

public class ShapeTagEmissionTests
{
    [Fact]
    public void ShapeTagIsCommentedOutByDefault()
    {
        string generatedView = GenerateView("[ZeroSerializer.ZeroSerializer] public class Record { public int Value { get; set; } }");

        Assert.DoesNotContain("/// <remarks>", generatedView);
        Assert.Contains("// To emit this, `set EmitShapeTag = true` on ZeroSerializerAttribute.", generatedView);
        Assert.Contains("// public const string ShapeTag = \"v1/{int}\";", generatedView);
        Assert.Contains("public const uint ShapeHash = ", generatedView);

        Assert.DoesNotContain("            public const string ShapeTag", generatedView);
    }

    [Fact]
    public void ShapeTagAndRemarksAreEmittedWhenRequested()
    {
        string generatedView = GenerateView("[ZeroSerializer.ZeroSerializer(EmitShapeTag = true)] public class Record { public int Value { get; set; } }");

        Assert.Contains("/// <remarks>", generatedView);
        Assert.Contains("/// ShapeTag: v1/{int}", generatedView);
        Assert.Contains("public const string ShapeTag = \"v1/{int}\";", generatedView);
        Assert.DoesNotContain("// public const string ShapeTag", generatedView);
        Assert.Contains("public const uint ShapeHash = ", generatedView);
    }

    [Fact]
    public void InjectedAttributeExposesObsoleteEmitShapeTagField()
    {
        GeneratorDriverRunResult result = RunGenerator("public class Record { }");
        string generatedAttribute = result.Results[0].GeneratedSources
            .Single(source => source.HintName == "- ZeroSerializerAttribute.g.cs")
            .SourceText
            .ToString();

        Assert.Contains("public bool EmitShapeTag;", generatedAttribute);
        Assert.Contains("[Obsolete(\"Emitting string representation of the type will expose internal details", generatedAttribute);
        Assert.Contains("public ZeroSerializerAttribute() { }", generatedAttribute);
    }

    private static string GenerateView(string source)
    {
        GeneratorDriverRunResult result = RunGenerator(source);
        return result.Results[0].GeneratedSources
            .Single(generatedSource => generatedSource.HintName == "Record.ZeroSerializer.g.cs")
            .SourceText
            .ToString();
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "ShapeTagEmissionTests",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location) });
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ZeroSerializerGenerator());
        return driver.RunGenerators(compilation).GetRunResult();
    }
}
