#include <assert.h>
#include "picoboot_connection.h"

#ifdef _WIN32
#include "libusb_windows_common_minimal.h"
#endif

#ifdef _WIN32
#define DLL_EXPORT extern "C" __declspec(dllexport)
#else
#define DLL_EXPORT
#endif

DLL_EXPORT enum memory_type PBC_get_memory_type(uint32_t addr, model_t model)
{
    return get_memory_type(addr, model);
}

DLL_EXPORT bool PBC_is_transfer_aligned(uint32_t addr, model_t model)
{
    return is_transfer_aligned(addr, model);
}

DLL_EXPORT bool PBC_is_size_aligned(uint32_t addr, int size)
{
    return is_size_aligned(addr, size) ;
}

// It is unfortunately not reasonably possible to correlate Windows SetupAPI devices with libusb devices
// There is a PR to add this (and more), but it's been stalled out for years now https://github.com/libusb/libusb/pull/537
// This is a hacky workaround to get at the instance ID, which lives in the private dev_id field of each device.
//
// Note that we can't just use the bus/address numbers like picotool does.
// Both values are basically fabricated by libusb through some very non-trivial logic and emergent behavior, they aren't something provided by Windows.
//
// Do note that libusb uses SetupDiGetDeviceInstanceIdA to get the instance ID, which is legacy and for whatever reason sometimes gives different
// casing compared to SetupDiGetDevicePropertyW w/ DEVPKEY_Device_InstanceId.
DLL_EXPORT const char* PBC_GetUsbInstanceId(libusb_device* device)
{
#ifdef _WIN32
    winusb_device_priv* priv = (winusb_device_priv*)usbi_get_device_priv(device);
    return priv->initialized ? priv->dev_id : nullptr;
#else
    return nullptr;
#endif
}

// Export picoboot functions
#ifdef _WIN32
#pragma comment(linker, "/export:picoboot_open_device")
#pragma comment(linker, "/export:picoboot_reset")
#pragma comment(linker, "/export:picoboot_cmd_status_verbose")
#pragma comment(linker, "/export:picoboot_cmd_status")
#pragma comment(linker, "/export:picoboot_exclusive_access")
#pragma comment(linker, "/export:picoboot_enter_cmd_xip")
#pragma comment(linker, "/export:picoboot_exit_xip")
#pragma comment(linker, "/export:picoboot_reboot")
#pragma comment(linker, "/export:picoboot_reboot2")
#pragma comment(linker, "/export:picoboot_get_info")
#pragma comment(linker, "/export:picoboot_exec")
#pragma comment(linker, "/export:picoboot_flash_erase")
#pragma comment(linker, "/export:picoboot_vector")
#pragma comment(linker, "/export:picoboot_write")
#pragma comment(linker, "/export:picoboot_read")
#pragma comment(linker, "/export:picoboot_otp_write")
#pragma comment(linker, "/export:picoboot_otp_read")
#pragma comment(linker, "/export:picoboot_poke")
#pragma comment(linker, "/export:picoboot_peek")
#pragma comment(linker, "/export:picoboot_flash_id")
#endif
