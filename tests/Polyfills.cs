// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

#if NET5_0_OR_GREATER == false

#pragma warning disable IDE0130  // Namespace does not match folder structure
namespace System.Runtime.CompilerServices
{
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}

#endif
