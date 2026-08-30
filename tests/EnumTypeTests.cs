// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using System;
using Xunit;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member
#pragma warning disable SMA8003  // Do not use debug-only `Assert` in public API surface

namespace ZeroSerializer.Tests.EnumTypingModels
{
    public enum ByteBackedState : byte
    {
        Ready = 1,
        Complete = 2,
    }

    public enum IntBackedMode : int
    {
        Passive = -1000,
        Active = 1000,
    }

    [Flags]
    public enum UlongBackedOptions : ulong
    {
        None = 0,
        First = 1UL << 40,
        Second = 1UL << 48,
        Audit = 1UL << 56,
    }

    [ZeroSerializer]
    public sealed class EnumClassContainer
    {
        public ByteBackedState ByteState { get; init; }

        public IntBackedMode IntMode { get; init; }

        public UlongBackedOptions UlongOptions { get; init; }
    }

    [ZeroSerializer]
    public readonly struct EnumStructContainer
    {
        public EnumStructContainer(
            ByteBackedState byteState,
            IntBackedMode intMode,
            UlongBackedOptions ulongOptions)
        {
            ByteState = byteState;
            IntMode = intMode;
            UlongOptions = ulongOptions;
        }

        public ByteBackedState ByteState { get; }

        public IntBackedMode IntMode { get; }

        public UlongBackedOptions UlongOptions { get; }
    }
}

namespace ZeroSerializer.Tests
{
    using EnumTypingModels;

    public sealed class EnumTypeTests
    {
        [Fact]
        public void ClassViewPreservesDeclaredEnumTypes()
        {
            EnumClassContainer source = new EnumClassContainer
            {
                ByteState = ByteBackedState.Complete,
                IntMode = IntBackedMode.Passive,
                UlongOptions = UlongBackedOptions.First | UlongBackedOptions.Audit,
            };
            byte[] serializedData = new byte[EnumClassContainerView.RequiredByteLength];

            int classWritten = source.Serialize(serializedData);
            EnumClassContainerView view = new EnumClassContainerView(serializedData);

            TestAssert.Equal(25, classWritten, nameof(classWritten));

            // Direct assignments intentionally prevent generated getters from regressing to underlying integer types.
            ByteBackedState byteState = view.ByteState;
            IntBackedMode intMode = view.IntMode;
            UlongBackedOptions ulongOptions = view.UlongOptions;

            Assert.Equal(source.ByteState, byteState);
            Assert.Equal(source.IntMode, intMode);
            Assert.Equal(source.UlongOptions, ulongOptions);
        }

        [Fact]
        public void StructViewPreservesDeclaredEnumTypes()
        {
            EnumStructContainer source = new EnumStructContainer(
                ByteBackedState.Ready,
                IntBackedMode.Active,
                UlongBackedOptions.Second | UlongBackedOptions.Audit);
            byte[] serializedData = new byte[EnumStructContainerView.RequiredByteLength];

            int structWritten = source.Serialize(serializedData);
            EnumStructContainerView view = new EnumStructContainerView(serializedData);

            TestAssert.Equal(25, structWritten, nameof(structWritten));

            // Direct assignments intentionally prevent generated getters from regressing to underlying integer types.
            ByteBackedState byteState = view.ByteState;
            IntBackedMode intMode = view.IntMode;
            UlongBackedOptions ulongOptions = view.UlongOptions;

            Assert.Equal(source.ByteState, byteState);
            Assert.Equal(source.IntMode, intMode);
            Assert.Equal(source.UlongOptions, ulongOptions);
        }
    }
}
