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
    public async Task ZEROS009_Violation_ClassWithStructLayout()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[{|#0:StructLayout(LayoutKind.Sequential, Pack = 1)|}]
[ZeroSerializer]
public class MyClassWithStructLayout
{
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS009", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithMessage("StructLayout attribute on class 'MyClassWithStructLayout' has no effect")
        );
    }

    [Fact]
    public async Task ZEROS009_Compliant_StructWithAttributes()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[ZeroSerializer]
public struct MyStructWithAttributes
{
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }

    [Fact]
    public async Task ZEROS009_Compliant_ClassWithoutZeroSerializer()
    {
        string source = @"
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class MyClassWithoutZeroSerializer
{
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }

    [Fact]
    public async Task ZEROS002_Violation_NonBlittableStructWithLayout()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[{|#0:StructLayout(LayoutKind.Sequential, Pack = 1)|}]
[ZeroSerializer]
public struct MyNonBlittableStruct
{
    private int _value;
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS002", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithMessage("Struct 'MyNonBlittableStruct' is marked with StructLayout(LayoutKind.Sequential, Pack = 1) but does not meet the requirements to be a blittable struct")
        );
    }

    [Fact]
    public async Task ZEROS002_Compliant_BlittableStructWithLayout()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[ZeroSerializer]
public struct MyBlittableStruct
{
    public int Value { get; set; }

    public MyBlittableStruct(int value) => Value = value;
    internal MyBlittableStruct(int value, int dummy) => Value = value;
    private MyBlittableStruct(string s) => Value = 0;

    public void PublicMethod() { }
    internal void InternalMethod() { }
    private void PrivateMethod() { }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source
        );
    }

    [Fact]
    public async Task ZEROS002_Compliant_NonBlittableStructWithoutLayout()
    {
        string source = @"
using ZeroSerializer;

[ZeroSerializer]
public struct MyNonBlittableStructWithoutLayout
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
    public async Task ZEROS002_Violation_PublicField()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[{|#0:StructLayout(LayoutKind.Sequential, Pack = 1)|}]
[ZeroSerializer]
public struct StructWithPublicField
{
    public int Field;
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS002", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithMessage("Struct 'StructWithPublicField' is marked with StructLayout(LayoutKind.Sequential, Pack = 1) but does not meet the requirements to be a blittable struct")
        );
    }

    [Fact]
    public async Task ZEROS002_Violation_NonPublicGetterProperty()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[{|#0:StructLayout(LayoutKind.Sequential, Pack = 1)|}]
[ZeroSerializer]
public struct StructWithNonPublicGetter
{
    public int PrivateGetter { private get; set; }
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS002", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithMessage("Struct 'StructWithNonPublicGetter' is marked with StructLayout(LayoutKind.Sequential, Pack = 1) but does not meet the requirements to be a blittable struct")
        );
    }

    [Fact]
    public async Task ZEROS002_Violation_PublicSetterOnlyProperty()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[{|#0:StructLayout(LayoutKind.Sequential, Pack = 1)|}]
[ZeroSerializer]
public struct StructWithSetterOnlyProperty
{
    public int SetterOnly { set { } }
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS002", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithMessage("Struct 'StructWithSetterOnlyProperty' is marked with StructLayout(LayoutKind.Sequential, Pack = 1) but does not meet the requirements to be a blittable struct")
        );
    }

    [Fact]
    public async Task ZEROS002_Violation_PublicInitOnlyProperty()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

[{|#0:StructLayout(LayoutKind.Sequential, Pack = 1)|}]
[ZeroSerializer]
public struct StructWithInitOnlyProperty
{
    public int InitOnly { init { } }
    public int Value { get; init; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS002", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithMessage("Struct 'StructWithInitOnlyProperty' is marked with StructLayout(LayoutKind.Sequential, Pack = 1) but does not meet the requirements to be a blittable struct")
        );
    }

    [Fact]
    public async Task ZEROS002_Violation_PartialStructWithLayout()
    {
        string source = @"
using System.Runtime.InteropServices;
using ZeroSerializer;

[{|#0:StructLayout(LayoutKind.Sequential, Pack = 1)|}]
[ZeroSerializer]
public partial struct MyPartialStruct
{
    public int Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS002", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithMessage("Struct 'MyPartialStruct' is marked with StructLayout(LayoutKind.Sequential, Pack = 1) but does not meet the requirements to be a blittable struct")
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
    public {|#0:PackedValue|} Value { get; set; }
    public {|#1:PackedValue?|} OptionalValue { get; set; }
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
    public async Task ZEROS003_Violation_UnmarkedNestedClass()
    {
        string source = @"
using ZeroSerializer;

public class UnmarkedClass
{
    public int Value { get; set; }
}

[ZeroSerializer]
public class Container
{
    public {|#0:UnmarkedClass|} Value { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS003", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithMessage("Field 'Value' has unsupported type 'UnmarkedClass'")
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
    public int Value { get; set; }
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
    public {|#0:PackedValue[]|} Values { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
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
    public int Value { get; set; }
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
    public {|#0:PackedValue|} Value { get; set; }
}

[ZeroSerializer]
public class Container
{
    public InvalidType {|#1:NestedValue|} { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
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
    public int Value { get; set; }
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
    public async Task ZEROS007_Violation_BoolProperty()
    {
        string source = @"
using ZeroSerializer;

[ZeroSerializer]
public class MyBoolClass
{
    public {|#0:bool|} IsActive { get; set; }
}
";

        await CSharpSourceGeneratorVerifier<ZeroSerializerGenerator>.VerifySourceGeneratorAsync(
            source,
            new DiagnosticResult("ZEROS007", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithMessage("Property 'IsActive' uses bool type; consider using a flags enum (byte) to reduce payload size by combining up to 8 booleans into one byte")
        );
    }

    [Fact]
    public async Task ZEROS007_Compliant_NonBoolProperty()
    {
        string source = @"
using ZeroSerializer;

[ZeroSerializer]
public class MyNonBoolClass
{
    public int Value { get; set; }
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
