// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;
using ZeroSerializer.Generator;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Tests.Diagnostics;

public class DiagnosticTests
{
    [Fact]
    public async Task ZEROS001_Violation_NestedType()
    {
        string source = @"
using ZeroSerializer;

public class Outer
{
    public Inner Value { get; set; }

    [ZeroSerializer]
    public class {|#0:Inner|}
    {
        public int Value { get; set; }
    }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS001", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithMessage("Type 'Outer.Inner' must be a top-level class or struct without a non-object base class")
        );
    }

    [Fact]
    public async Task ZEROS001_Compliant_TopLevelType()
    {
        string source = @"
using ZeroSerializer;

public class Outer
{
    public TopLevelInner Value { get; set; }
}

[ZeroSerializer]
public class TopLevelInner
{
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }

    [Fact]
    public async Task ZEROS009_Violation_UnmarkedTypeReturnedByProperty()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

public class UnmarkedClass
{
    public int Value { get; set; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UnmarkedStruct
{
    public int Value;
}

[ZeroSerializer]
public class Container
{
    public {|#0:UnmarkedClass|} {|#1:Value|} { get; set; }
    public {|#2:UnmarkedStruct|} {|#3:StructValue|} { get; set; }
    public {|#4:UnmarkedStruct?|} {|#5:NullableStructValue|} { get; set; }
    public {|#6:UnmarkedStruct[]|} {|#7:StructArrayValue|} { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS009", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithMessage("Property 'Value' returns type 'UnmarkedClass' that is not marked with [ZeroSerializer]"),
            new DiagnosticResult("ZEROS003", DiagnosticSeverity.Error)
                .WithLocation(1)
                .WithMessage("Field 'Value' has unsupported type 'UnmarkedClass'"),

            new DiagnosticResult("ZEROS009", DiagnosticSeverity.Warning)
                .WithLocation(2)
                .WithMessage("Property 'StructValue' returns type 'UnmarkedStruct' that is not marked with [ZeroSerializer]"),
            new DiagnosticResult("ZEROS003", DiagnosticSeverity.Error)
                .WithLocation(3)
                .WithMessage("Field 'StructValue' has unsupported type 'UnmarkedStruct'"),

            new DiagnosticResult("ZEROS009", DiagnosticSeverity.Warning)
                .WithLocation(4)
                .WithMessage("Property 'NullableStructValue' returns type 'UnmarkedStruct' that is not marked with [ZeroSerializer]"),
            new DiagnosticResult("ZEROS003", DiagnosticSeverity.Error)
                .WithLocation(5)
                .WithMessage("Field 'NullableStructValue' has unsupported type 'UnmarkedStruct?'"),

            new DiagnosticResult("ZEROS009", DiagnosticSeverity.Warning)
                .WithLocation(6)
                .WithMessage("Property 'StructArrayValue' returns type 'UnmarkedStruct' that is not marked with [ZeroSerializer]"),
            new DiagnosticResult("ZEROS004", DiagnosticSeverity.Error)
                .WithLocation(7)
                .WithMessage("Array field 'StructArrayValue' requires a primitive, enum, or a [ZeroSerializer] struct recursively marked with StructLayout(LayoutKind.Sequential, Pack = 1)")
        );
    }

    [Fact]
    public async Task ZEROS009_Compliant_MarkedTypeReturnedByProperty()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[ZeroSerializer]
public class MarkedClass
{
    public int Value { get; set; }
}

[ZeroSerializer]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MarkedStruct
{
    public int Value;
}

[ZeroSerializer]
public class Container
{
    public MarkedClass Value { get; set; }
    public MarkedStruct StructValue { get; set; }
    public MarkedStruct? NullableStructValue { get; set; }
    public MarkedStruct[] StructArrayValue { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }

    [Fact]
    public async Task ZEROS002_Compliant_InaccessibleField()
    {
        string source = @"
using ZeroSerializer;

[ZeroSerializer]
public class ClassWithPrivateField
{
    private int _value;
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }

    [Fact]
    public async Task ZEROS003_Violation_UnmarkedBlittableNestedStruct()
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
    public {|#2:PackedValue|} {|#0:Value|} { get; set; }
    public {|#3:PackedValue?|} {|#1:OptionalValue|} { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS009", DiagnosticSeverity.Warning)
                .WithLocation(2)
                .WithMessage("Property 'Value' returns type 'PackedValue' that is not marked with [ZeroSerializer]"),
            new DiagnosticResult("ZEROS003", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithMessage("Field 'Value' has unsupported type 'PackedValue'"),
            new DiagnosticResult("ZEROS009", DiagnosticSeverity.Warning)
                .WithLocation(3)
                .WithMessage("Property 'OptionalValue' returns type 'PackedValue' that is not marked with [ZeroSerializer]"),
            new DiagnosticResult("ZEROS003", DiagnosticSeverity.Error)
                .WithLocation(1)
                .WithMessage("Field 'OptionalValue' has unsupported type 'PackedValue?'")
        );
    }

    [Fact]
    public async Task ZEROS003_Compliant_SupportedFields()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[ZeroSerializer]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedValue
{
    public int Value;
}

[ZeroSerializer]
public class Container
{
    public PackedValue Value { get; set; }
    public PackedValue? OptionalValue { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }

    [Fact]
    public async Task ZEROS004_Violation_UnmarkedBlittableArrayElement()
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
    public {|#1:PackedValue[]|} {|#0:Values|} { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS009", DiagnosticSeverity.Warning)
                .WithLocation(1)
                .WithMessage("Property 'Values' returns type 'PackedValue' that is not marked with [ZeroSerializer]"),
            new DiagnosticResult("ZEROS004", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithMessage("Array field 'Values' requires a primitive, enum, or a [ZeroSerializer] struct recursively marked with StructLayout(LayoutKind.Sequential, Pack = 1)")
        );
    }

    [Fact]
    public async Task ZEROS004_Compliant_SupportedArrayElement()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[ZeroSerializer]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedValue
{
    public int Value;
}

[ZeroSerializer]
public class Container
{
    public PackedValue[] Values { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }

    [Fact]
    public async Task ZEROS005_Violation_InvalidNestedSerializableType()
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
public class InvalidType
{
    public {|#2:PackedValue|} {|#0:Value|} { get; set; }
}

[ZeroSerializer]
public class Container
{
    public InvalidType {|#1:NestedValue|} { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS009", DiagnosticSeverity.Warning)
                .WithLocation(2)
                .WithMessage("Property 'Value' returns type 'PackedValue' that is not marked with [ZeroSerializer]"),
            new DiagnosticResult("ZEROS003", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithMessage("Field 'Value' has unsupported type 'PackedValue'"),
            new DiagnosticResult("ZEROS005", DiagnosticSeverity.Error)
                .WithLocation(1)
                .WithMessage("Field 'NestedValue' refers to serializable type 'InvalidType', but that type contains errors")
        );
    }

    [Fact]
    public async Task ZEROS005_Compliant_ValidNestedSerializableType()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[ZeroSerializer]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedValue
{
    public int Value;
}

[ZeroSerializer]
public class ValidType
{
    public PackedValue Value { get; set; }
}

[ZeroSerializer]
public class Container
{
    public ValidType NestedValue { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }

    [Fact]
    public async Task ZEROS006_Violation_StructWithoutLayout()
    {
        string source = @"
using ZeroSerializer;

[ZeroSerializer]
public struct {|#0:MyStruct|}
{
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS006", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithMessage("Struct 'MyStruct' has a Blittable-compatible field shape; use StructLayout(LayoutKind.Sequential, Pack = 1) to enable raw payload serialization")
        );
    }

    [Fact]
    public async Task ZEROS006_Compliant_StructWithLayout()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[ZeroSerializer]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MyStructWithLayout
{
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }

    [Fact]
    public async Task ZEROS007_Violation_BlittableCompatibleNestedStruct()
    {
        string source = @"
using ZeroSerializer;

[ZeroSerializer]
public struct {|#0:NestedStruct|}
{
    public int Value { get; set; }
}

[ZeroSerializer]
public class ParentClass
{
    public NestedStruct Child { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS006", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithMessage("Struct 'NestedStruct' has a Blittable-compatible field shape; use StructLayout(LayoutKind.Sequential, Pack = 1) to enable raw payload serialization"),
            new DiagnosticResult("ZEROS007", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithMessage("Nested struct 'NestedStruct' can use StructLayout(LayoutKind.Sequential, Pack = 1) to improve serialization performance with raw payload serialization")
        );
    }

    [Fact]
    public async Task ZEROS007_Compliant_BlittableNestedStructWithLayout()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[ZeroSerializer]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NestedStructWithLayout
{
    public int Value { get; set; }
}

[ZeroSerializer]
public class ParentClassWithBlittable
{
    public NestedStructWithLayout Child { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }

    [Fact]
    public async Task ZEROS008_Violation_GenericType()
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
    public async Task ZEROS008_Compliant_NonGenericType()
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
