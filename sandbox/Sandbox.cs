// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using System;
using System.Runtime.InteropServices;
using ZeroSerializer;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member
#pragma warning disable CEK001  // Collection expressions are not allowed
#pragma warning disable CEK005  // Collection expressions must be empty

var fixedPacket = new FixedPacket
{
    FloatValue = 1.5f,
    IntegerValue = 42,
    State = PacketState.Ready,
    Position = new PackedPosition { X = 10, Y = 20 },
};
var fixedBuffer = new byte[FixedPacketView.RequiredByteLength];
fixedPacket.Serialize(fixedBuffer);

var fixedView = new FixedPacketView(fixedBuffer);
if (fixedView.FloatValue != fixedPacket.FloatValue
    || fixedView.IntegerValue != fixedPacket.IntegerValue
    || fixedView.State != fixedPacket.State
    || fixedView.Position.X != fixedPacket.Position.X
    || fixedView.Position.Y != fixedPacket.Position.Y)
{
    throw new InvalidOperationException("Fixed-size View did not match its source.");
}

var variablePacket = new VariablePacket
{
    Name = "sandbox",
    Values = [10, 20, 30],
    OptionalValue = null,
    Child = new ChildPacket { Identifier = 99 },
};
var variableBuffer = new byte[256];
variablePacket.Serialize(variableBuffer);

var variableView = new VariablePacketView(variableBuffer);
if (VariablePacketView.RequiredByteLength != -1
    || !variableView.Name.SequenceEqual(variablePacket.Name)
    || variableView.Values.Length != variablePacket.Values.Length
    || variableView.Values[2] != variablePacket.Values[2]
    || variableView.OptionalValue is not null
    || variableView.Child.Identifier != variablePacket.Child.Identifier)
{
    throw new InvalidOperationException("Runtime-sized View did not match its source.");
}

// Keeping these cases together catches wire-offset regressions when a null payload is followed by another property.
Console.WriteLine("ZeroSerializer sandbox passed.");

[ZeroSerializer]
public struct FixedPacket
{
    public float FloatValue { get; init; }

    public int IntegerValue { get; init; }

    public PacketState State { get; init; }

    public PackedPosition Position { get; init; }
}

public enum PacketState : ushort
{
    None,
    Ready,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedPosition
{
    public int X;

    public int Y;
}

[ZeroSerializer]
public sealed class VariablePacket
{
    public string? Name { get; init; }

    public int[]? Values { get; init; }

    public int? OptionalValue { get; init; }

    public ChildPacket? Child { get; init; }
}

[ZeroSerializer]
public sealed class ChildPacket
{
    public int Identifier { get; init; }
}

// Polyfill
namespace System.Runtime.CompilerServices
{
    struct IsExternalInit { }
}
