using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Harp.Devices.SetupApi;

unsafe partial class Globals
{
    public delegate bool SetupDiGetDevicePropertyTConverter<T>(DEVPROPTYPE type, ReadOnlySpan<byte> data, [MaybeNullWhen(false)] out T result);

    public static bool TryGetDevicePropertyT<T>(HDEVINFO deviceList, SP_DEVINFO_DATA* deviceInfo, scoped in DEVPROPKEY key, [MaybeNullWhen(false)] out T result, SetupDiGetDevicePropertyTConverter<T> converter)
    {
        fixed (DEVPROPKEY* keyP = &key)
        {
            // Figure out how large of a buffer is needed to read the property
            DEVPROPTYPE type = DEVPROPTYPE.EMPTY;
            uint bufferSize = 0;
            BOOL success = SetupDiGetDevicePropertyW(deviceList, deviceInfo, keyP, &type, null, 0, &bufferSize, 0);
            Win32Error lastError = Windows.GetLastError();
            success.AssertFalse(); // This call isn't expected to succeed

            switch (lastError)
            {
                // Typical case when property exists
                case Win32Error.ERROR_INSUFFICIENT_BUFFER:
                    break;

                // Typical case when property does not exist
                case Win32Error.ERROR_NOT_FOUND:
                    result = default;
                    return false;

                // Programming errors
                case Win32Error.ERROR_INVALID_FLAGS:
                case Win32Error.ERROR_INVALID_HANDLE:
                case Win32Error.ERROR_INVALID_PARAMETER:
                case Win32Error.ERROR_INVALID_REG_PROPERTY:
                case Win32Error.ERROR_INVALID_USER_BUFFER:
                    Trace.WriteLine($"{nameof(SetupDiGetDevicePropertyW)} failed with {lastError}, which indicates an internal bug.");
                    Debug.Fail($"Internal error in {nameof(TryGetDevicePropertyT)}!");
                    result = default;
                    return false;

                // Admin is needed (is this ever actually expected for reading device instance properties?)
                case Win32Error.ERROR_ACCESS_DENIED:
                // Device doesn't exist
                case Win32Error.ERROR_NO_SUCH_DEVINST:
                // SetupAPI internal errors
                case Win32Error.ERROR_NOT_ENOUGH_MEMORY:
                case Win32Error.ERROR_INVALID_DATA:
                default:
                    Trace.WriteLine($"{nameof(SetupDiGetDevicePropertyW)} failed with {lastError} when reading {key.fmtid}, which is not typical.");
                    result = default;
                    return false;
            }

            int bufferSizeI = checked((int)bufferSize);
            Span<byte> buffer = bufferSize <= 4096 ? stackalloc byte[bufferSizeI] : GC.AllocateUninitializedArray<byte>(bufferSizeI, pinned: true);
            success = SetupDiGetDevicePropertyW(deviceList, deviceInfo, keyP, &type, (byte*)Unsafe.AsPointer(ref buffer[0]), bufferSize, null, 0);
            lastError = Windows.GetLastError();
            success.AssertTrue(); // It's not expected that the buffer sizing call is effectively successful while this one isn't

            if (!success)
            {
                Trace.WriteLine($"Second call to {nameof(SetupDiGetDevicePropertyW)} failed with {lastError} when reading {key.fmtid} even though buffer sizing call succeeded.");
                result = default;
                return false;
            }

            return converter(type, buffer, out result);
        }
    }

    public static bool TryGetDevicePropertyT<T>(HDEVINFO deviceList, SP_DEVINFO_DATA* deviceInfo, scoped in DEVPROPKEY key, DEVPROPTYPE expectedType, out T result)
        where T : unmanaged
        => TryGetDevicePropertyT<T>
        (
            deviceList,
            deviceInfo,
            key,
            out result,
            (DEVPROPTYPE type, ReadOnlySpan<byte> data, out T result) =>
            {
                result = default;

                if (type != expectedType)
                    return false;

                ReadOnlySpan<T> tSpan = MemoryMarshal.Cast<byte, T>(data);
                Debug.Assert(tSpan.Length == 1);
                result = tSpan[0];
                return true;
            }
        );

    public static string? TryGetDevicePropertyString(HDEVINFO deviceList, SP_DEVINFO_DATA* deviceInfo, in DEVPROPKEY key)
        => TryGetDevicePropertyT<string>
        (
            deviceList,
            deviceInfo,
            key,
            out string? result,
            static (DEVPROPTYPE type, ReadOnlySpan<byte> data, [MaybeNullWhen(false)] out string result) =>
            {
                result = null;

                if (type == DEVPROPTYPE.STRING_INDIRECT)
                    throw new NotImplementedException();

                if (type != DEVPROPTYPE.STRING)
                    return false;

                ReadOnlySpan<char> charSpan = MemoryMarshal.Cast<byte, char>(data);

                int nullTerminator = charSpan.IndexOf('\0');
                if (nullTerminator >= 0)
                    charSpan = charSpan.Slice(0, nullTerminator);

                result = charSpan.ToString();
                return true;
            }
        ) ? result : null;

    public static ImmutableArray<string> TryGetDevicePropertyStringList(HDEVINFO deviceList, SP_DEVINFO_DATA* deviceInfo, in DEVPROPKEY key)
        => TryGetDevicePropertyT<ImmutableArray<string>>
        (
            deviceList,
            deviceInfo,
            key,
            out ImmutableArray<string> result,
            static (DEVPROPTYPE type, ReadOnlySpan<byte> data, [MaybeNullWhen(false)] out ImmutableArray<string> result) =>
            {
                result = ImmutableArray<string>.Empty;

                if (type.Type == DEVPROP_TYPE.STRING_INDIRECT)
                    throw new NotImplementedException();

                if (type != DEVPROPTYPE.STRING_LIST)
                    return false;

                ReadOnlySpan<char> charSpan = MemoryMarshal.Cast<byte, char>(data);
                ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();

                while (charSpan.Length > 0 && charSpan[0] != '\0')
                {
                    int nullTerminator = charSpan.IndexOf('\0');
                    if (nullTerminator <= 0)
                    {
                        Debug.Fail("The string list is malformed.");
                        break;
                    }

                    string substring = charSpan.Slice(0, nullTerminator).ToString();
                    builder.Add(substring);
                    charSpan = charSpan.Slice(nullTerminator + 1);
                }

                result = builder.ToImmutable();
                return true;
            }
        ) ? result : ImmutableArray<string>.Empty;

    public static bool TryGetDevicePropertyGuid(HDEVINFO deviceList, SP_DEVINFO_DATA* deviceInfo, in DEVPROPKEY key, out Guid value)
        => TryGetDevicePropertyT<Guid>
        (
            deviceList,
            deviceInfo,
            key,
            out value,
            static (DEVPROPTYPE type, ReadOnlySpan<byte> data, out Guid result) =>
            {
                result = default;

                if (type != DEVPROPTYPE.GUID)
                    return false;

                ReadOnlySpan<Guid> guidSpan = MemoryMarshal.Cast<byte, Guid>(data);
                Debug.Assert(guidSpan.Length == 1);
                result = guidSpan[0];
                return true;
            }
        );

    public static Guid? TryGetDevicePropertyGuid(HDEVINFO deviceList, SP_DEVINFO_DATA* deviceInfo, in DEVPROPKEY key)
        => TryGetDevicePropertyGuid(deviceList, deviceInfo, key, out Guid result) ? result : null;
}
