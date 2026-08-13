// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Xunit;
using ZeroSerializer;
using ZeroSerializer.Tests.Models;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Tests;

public class ShapeTagAndHashTests
{
    [Fact]
    public void PrimitiveRecordViewTest()
    {
        string primitiveExpected = "v1/{bool,byte,sbyte,char,short,ushort,int,uint,long,ulong,float,double}";
        Assert.Equal(PrimitiveRecordView.ShapeTag, primitiveExpected);
        Assert.Equal(PrimitiveRecordView.ShapeHash, XXHash32.HashToUInt32(primitiveExpected));
    }

    [Fact]
    public void PackedRecordViewTest()
    {
        string packedRecordExpected = "v1/blittable{int,enum:short}";
        Assert.Equal(PackedRecordView.ShapeTag, packedRecordExpected);
        Assert.Equal(PackedRecordView.ShapeHash, XXHash32.HashToUInt32(packedRecordExpected));

    }

    [Fact]
    public void PackedContainerViewTest()
    {
        string packedContainerExpected = "v1/{blittable{int,enum:short},blittable{int,enum:short}?,blittable{int,enum:short}[]}";
        Assert.Equal(PackedContainerView.ShapeTag, packedContainerExpected);
        Assert.Equal(PackedContainerView.ShapeHash, XXHash32.HashToUInt32(packedContainerExpected));
    }

    [Fact]
    public void EnumClassViewTest()
    {
        string enumClassExpected = "v1/{enum:byte,enum:short,enum:int,enum:int?}";
        Assert.Equal(EnumClassView.ShapeTag, enumClassExpected);
        Assert.Equal(EnumClassView.ShapeHash, XXHash32.HashToUInt32(enumClassExpected));
    }

    [Fact]
    public void SchemaSignatureTestsModelViewTest()
    {
        string schemaSignatureExpected = "v1/{blittable{int,enum:short},{int,enum:byte},blittable{int,enum:short}[],{},blittable{},{enum:byte,enum:short}?,enum:byte[],enum:ulong[],int?,bool?}";
        Assert.Equal(SchemaSignatureTestsModelView.ShapeTag, schemaSignatureExpected);
        Assert.Equal(SchemaSignatureTestsModelView.ShapeHash, XXHash32.HashToUInt32(schemaSignatureExpected));
    }
}
