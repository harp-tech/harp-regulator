using System;

namespace PicobootConnection;

public sealed class PicobootCommandFailureException : Exception
{
    public readonly picoboot_status Status;

    public PicobootCommandFailureException(string? messagePrefix, picoboot_status status)
        : base($"{messagePrefix}: {status}")
        => Status = status;
}
