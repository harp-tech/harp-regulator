using System;

namespace Harp.Devices.SetupApi;

/// <summary>Dummy type used to provide conversions from <c>null</c> literals to handles</summary>
/// <remarks>Unless you are using it for its intended purpose, you should never need to reference this type.</remarks>
internal sealed class NullReference
{
    private NullReference()
        => throw new InvalidOperationException("Instances of this type must never be constructed.");
}
