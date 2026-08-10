// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.Threading.Tasks;
using Xunit;
using ZeroSerializer.Generator;
using ZeroSerializer.Tests.Verifiers;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Tests;

public class DiagnosticTests
{
    [Fact]
    public async Task TestGenericTypeDiagnostic()
    {
        string source = @"
using ZeroSerializer;

[ZeroSerializer]
public class {|#0:MyGenericClass|}<T>
{
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS008", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithMessage("Generic type 'MyGenericClass<T>' is not allowed for ZeroSerializer")
        );
    }

    [Fact]
    public async Task TestNonGenericTypeNoDiagnostic()
    {
        string source = @"
using ZeroSerializer;

[ZeroSerializer]
public class MyNonGenericClass
{
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }
}
