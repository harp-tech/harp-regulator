using System.ComponentModel;

namespace Harp.Devices.SetupApi;

internal static class Win32ErrorEx
{
    public static Win32Exception GetException(this Win32Error error)
        => new Win32Exception((int)error);

    public static void Throw(this Win32Error error)
        => throw error.GetException();

    public static void ThrowIfFailure(this Win32Error error)
    {
        if (error.IsFailure())
        { throw error.GetException(); }
    }

    public static string GetMessage(this Win32Error error)
        => error.GetException().Message;

    public static bool IsSuccess(this Win32Error error)
        => error == Win32Error.ERROR_SUCCESS;

    public static bool IsFailure(this Win32Error error)
        => error != Win32Error.ERROR_SUCCESS;
}

internal enum Win32Error : int
{
    /// <summary>The operation completed successfully.</summary>
    ERROR_SUCCESS = 0,
    /// <summary>The system cannot find the file specified.</summary>
    ERROR_FILE_NOT_FOUND = 2,
    /// <summary>Access is denied.</summary>
    ERROR_ACCESS_DENIED = 5,
    /// <summary>The handle is invalid.</summary>
    ERROR_INVALID_HANDLE = 6,
    /// <summary>Not enough memory resources are available to process this command.</summary>
    ERROR_NOT_ENOUGH_MEMORY = 8,
    /// <summary>The data is invalid.</summary>
    ERROR_INVALID_DATA = 13,
    /// <summary>The parameter is incorrect.</summary>
    ERROR_INVALID_PARAMETER = 87,
    /// <summary>The data area passed to a system call is too small.</summary>
    ERROR_INSUFFICIENT_BUFFER = 122,
    /// <summary>No more data is available.</summary>
    ERROR_NO_MORE_ITEMS = 259,
    /// <summary>Invalid flags.</summary>
    ERROR_INVALID_FLAGS = 1004,
    /// <summary>Element not found.</summary>
    ERROR_NOT_FOUND = 1168,
    /// <summary>The supplied user buffer is not valid for the requested operation.</summary>
    ERROR_INVALID_USER_BUFFER = 1784,

    ERROR_INVALID_REG_PROPERTY = unchecked((int)(0x20000000 | 0xC0000000 | 0x209)),
    ERROR_NO_SUCH_DEVINST = unchecked((int)(0x20000000 | 0xC0000000 | 0x20B)),
}
