// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

#if NETSTANDARD2_0

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
        public bool ReturnValue { get; }
    }
}

#endif
