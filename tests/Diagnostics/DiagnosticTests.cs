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
                .WithArguments("Outer.Inner")
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
    public async Task ZEROS103_Violation_ClassWithStructLayout()
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
            new DiagnosticResult("ZEROS103", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("MyClassWithStructLayout")
        );
    }

    [Fact]
    public async Task ZEROS103_Compliant_StructWithAttributes()
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
    public async Task ZEROS103_Compliant_ClassWithoutZeroSerializer()
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
    public async Task ZEROS101_Violation_NonBlittableStructWithLayout()
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
            new DiagnosticResult("ZEROS101", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("MyNonBlittableStruct")
        );
    }

    [Fact]
    public async Task ZEROS101_Compliant_BlittableStructWithLayout()
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
    public async Task ZEROS101_Compliant_NonBlittableStructWithoutLayout()
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
    public async Task ZEROS101_Violation_PublicField()
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
            new DiagnosticResult("ZEROS101", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("StructWithPublicField")
        );
    }

    [Fact]
    public async Task ZEROS101_Violation_NonPublicGetterProperty()
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
            new DiagnosticResult("ZEROS101", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("StructWithNonPublicGetter")
        );
    }

    [Fact]
    public async Task ZEROS101_Violation_PublicSetterOnlyProperty()
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
            new DiagnosticResult("ZEROS101", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("StructWithSetterOnlyProperty")
        );
    }

    [Fact]
    public async Task ZEROS101_Violation_PublicInitOnlyProperty()
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
            new DiagnosticResult("ZEROS101", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("StructWithInitOnlyProperty")
        );
    }

    [Fact]
    public async Task ZEROS101_Violation_PartialStructWithLayout()
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
            new DiagnosticResult("ZEROS101", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("MyPartialStruct")
        );
    }

    [Fact]
    public async Task ZEROS002_Violation_UnmarkedBlittableNestedStruct()
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
            new DiagnosticResult("ZEROS002", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("Value", "PackedValue"),
            new DiagnosticResult("ZEROS002", DiagnosticSeverity.Error)
                .WithLocation(1)
                .WithArguments("OptionalValue", "PackedValue?")
        );
    }

    [Fact]
    public async Task ZEROS002_Violation_UnmarkedNestedClass()
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
            new DiagnosticResult("ZEROS002", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("Value", "UnmarkedClass")
        );
    }

    [Fact]
    public async Task ZEROS002_Compliant_SupportedFields()
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
    public async Task ZEROS003_Violation_UnmarkedBlittableArrayElement()
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
            new DiagnosticResult("ZEROS003", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("Values")
        );
    }

    [Fact]
    public async Task ZEROS003_Compliant_SupportedArrayElement()
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
    public async Task ZEROS004_Violation_InvalidNestedSerializableType()
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
            new DiagnosticResult("ZEROS002", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("Value", "PackedValue"),
            new DiagnosticResult("ZEROS004", DiagnosticSeverity.Error)
                .WithLocation(1)
                .WithArguments("NestedValue", "InvalidType")
        );
    }

    [Fact]
    public async Task ZEROS004_Compliant_ValidNestedSerializableType()
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
    public async Task ZEROS102_Violation_StructWithoutLayout()
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
            new DiagnosticResult("ZEROS102", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("MyStruct")
        );
    }

    [Fact]
    public async Task ZEROS102_Compliant_StructWithLayout()
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
    public async Task ZEROS201_Violation_BoolProperty()
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
            new DiagnosticResult("ZEROS201", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("IsActive")
        );
    }

    [Fact]
    public async Task ZEROS201_Compliant_NonBoolProperty()
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
    public async Task ZEROS005_Violation_GenericType()
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
            new DiagnosticResult("ZEROS005", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("MyGenericClass<T>")
        );
    }

    [Fact]
    public async Task ZEROS005_Compliant_NonGenericType()
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
