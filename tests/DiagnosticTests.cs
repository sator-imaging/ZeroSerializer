// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;
using Xunit;
using ZeroSerializer.Generator;
using ZeroSerializer.Tests.Verifiers;

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

        CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.Verify(
            source,
            "ZEROS008",
            DiagnosticSeverity.Error,
            "MyGenericClass",
            "MyGenericClass"
        );
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

        CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifyNoDiagnostics(
            source
        );
    }
}
