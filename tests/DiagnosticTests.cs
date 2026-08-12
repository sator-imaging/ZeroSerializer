// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
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

    [Fact]
    public async Task TestUnmarkedBlittableNestedStructDiagnostic()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedValue
{
    public int Value;
}

[ZeroSerializer]
public class Container
{
    public PackedValue {|#0:Value|} { get; set; }
    public PackedValue? {|#1:OptionalValue|} { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS003", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithMessage("Field 'Value' has unsupported type 'PackedValue'"),
            new DiagnosticResult("ZEROS003", DiagnosticSeverity.Error)
                .WithLocation(1)
                .WithMessage("Field 'OptionalValue' has unsupported type 'PackedValue?'")
        );
    }

    [Fact]
    public async Task TestUnmarkedBlittableArrayElementDiagnostic()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedValue
{
    public int Value;
}

[ZeroSerializer]
public class Container
{
    public PackedValue[] {|#0:Values|} { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS004", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithMessage("Array field 'Values' requires a primitive, enum, or a [ZeroSerializer] struct recursively marked with StructLayout(LayoutKind.Sequential, Pack = 1)")
        );
    }
}
