<div align="center">

# ZeroSerializer

Zero-copy, Zero-allocation, Deserialize-on-Read

[![NuGet](https://img.shields.io/nuget/vpre/SatorImaging.ZeroSerializer)](https://www.nuget.org/packages/SatorImaging.ZeroSerializer)
&nbsp;
[![🇺🇸](https://img.shields.io/badge/🇺🇸-English-789)](./README.md)
[![🇯🇵](https://img.shields.io/badge/🇯🇵-日本語_※詳説-789)](./README.ja.md)

</div>


`ZeroSerializer` is a C# source generator for reading serialized data directly from an existing `byte[]`.


## Why ZeroSerializer?

Receiving data over a network, reading a file, or calling another API often leaves you with an unavoidable `byte[]` allocation. **ZeroSerializer** generates a read-only view like `ReadOnlySpan<T>` or `ReadOnlyMemory<T>` that reuses that allocation and provides strongly typed values and slices without creating another buffer or a deserialized object graph. Each property is read only when accessed, and strings and arrays remain borrowed from the original memory.





# Usage

```csharp
// Some APIs require data to be received into a byte array.
byte[] receivedBuffer = ReceivePacket();

// Like ReadOnlySpan<T> or ReadOnlyMemory<T>,
// a view provides strongly typed access over existing memory without owning it.
var packetView = new PacketView(receivedBuffer.AsMemory());

// Each property is decoded directly from the original buffer only when accessed.
int id = packetView.Id;
ReadOnlySpan<char> name = packetView.Name;
ReadOnlySpan<int> values = packetView.Values;

// Adding attribute to generate readonly struct 'PacketView'.
[ZeroSerializer]
public class Packet
{
    public int Id { get; }
    public string Name { get; }
    public int[] Values { get; }
}

// Serializing data into a buffer using generated 'Serialize' extension method.
var packet = new Packet { Id = 1, Name = "John", Values = new[] { 10, 20 } };
var buffer = new byte[256];
int writtenBytes = packet.Serialize(buffer);
```

View construction does not read every property. Values are decoded directly from the original memory only on access.

The complete serialized region is also available through implicit conversion:

```csharp
ReadOnlySpan<byte> serializedData = packetView;
ReadOnlyMemory<byte> retainedData = packetView;
```


## Blittable Structs

Structs marked with both `[ZeroSerializer]` and `[StructLayout(LayoutKind.Sequential, Pack = 1)]` are recognized as blittable structs.

Blittable structs provide significant benefits:
- **No Offset Table**: Serialized directly as raw memory payloads without field offset overhead.
- **Maximum Performance**: Fast direct memory copy operations with zero serialization/deserialization overhead.

```csharp
[ZeroSerializer]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Transform
{
    public float PositionX;
    public float PositionY;
    public float PositionZ;
}

// Serializing a blittable struct (exact fixed byte size available via RequiredByteLength)
var transform = new Transform { PositionX = 1.0f, PositionY = 2.0f, PositionZ = 3.0f };
var buffer = new byte[TransformView.RequiredByteLength];  // 12 bytes
int writtenBytes = transform.Serialize(buffer);

// Reading via View or materializing back to the original struct
var view = new TransformView(buffer);
Transform original = view.Materialize();
```





# Supported values

- Primitives and enums
- Nullable values
- `string` stored as UTF-16 (UTF-8 can be stored as byte[] by hand)
- Nested `[ZeroSerializer]` types
- Blittable structs with `[StructLayout(LayoutKind.Sequential, Pack = 1)]`
- One-dimensional arrays of blittable values or structs





# Serialized layout

Non-Blittable types store one relative offset per property, followed by payloads in declaration order:

```text
[ int property offsets... ][ property payloads... ]
```

Offsets are relative to the start of the containing type. An offset of `0` represents `null`. Strings and arrays store their byte length before their data:

```text
[ int byte length ][ data... ]
```

Blittable structs are stored directly as raw struct bytes without an offset table.





# Notes

- Keep the original memory alive and unchanged while its View is in use. This is the same rule as for `Span<T>` and `Memory<T>`.
- `RequiredByteLength` is the exact size (including the offset table) unless it is negative. A negative value indicates that the type contains variable-length data, such as strings or arrays. Passing the exact serialized region is recommended, but View access only requires the correct starting position.
- Validate integrity or authenticity before creating a View when required.
- The wire format requires a little-endian runtime.
- View structs expose a compile-time constant `IsBlittable`, indicating whether the underlying serialized type is a blittable struct.
- You can use `.AsMemory()` extension method (returns `ReadOnlyMemory<byte>`) or `.Materialize()` extension method (for views of blittable structs to convert them back to the original struct).
- Nested classes and structs must be marked with `[ZeroSerializer]`; otherwise the generator reports an unsupported field diagnostic.
