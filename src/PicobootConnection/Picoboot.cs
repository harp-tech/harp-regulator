using PicobootConnection.LibUsb;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PicobootConnection;

public unsafe static class Picoboot
{
    public const ushort VENDOR_ID_RASPBERRY_PI = (ushort)0x2e8au;
    public const ushort PRODUCT_ID_RP2040_USBBOOT = (ushort)0x0003u;
    public const ushort PRODUCT_ID_PICOPROBE = (ushort)0x0004u;
    public const ushort PRODUCT_ID_MICROPYTHON = (ushort)0x0005u;
    public const ushort PRODUCT_ID_STDIO_USB = (ushort)0x0009u;
    public const ushort PRODUCT_ID_RP2040_STDIO_USB = (ushort)0x000au;
    public const ushort PRODUCT_ID_RP2350_USBBOOT = (ushort)0x000fu;

    public const int LOG2_PAGE_SIZE = 8;
    public const uint PAGE_SIZE = (1u << LOG2_PAGE_SIZE);
    public const uint FLASH_SECTOR_ERASE_SIZE = 4096u;

    static Picoboot()
    {
        Debug.Assert(sizeof(picoboot_cmd_status) == 16);
    }

    [DllImport("PicobootConnection.Native")] public static extern picoboot_device_result picoboot_open_device(libusb_device device, libusb_device_handle* dev_handle, model_t* model, int vid, int pid, byte* ser);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_reset(libusb_device_handle usb_device);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_cmd_status_verbose(libusb_device_handle usb_device, picoboot_cmd_status* status, byte local_verbose);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_cmd_status(libusb_device_handle usb_device, picoboot_cmd_status* status);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_exclusive_access(libusb_device_handle usb_device, picoboot_exclusive_type exclusive);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_enter_cmd_xip(libusb_device_handle usb_device);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_exit_xip(libusb_device_handle usb_device);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_reboot(libusb_device_handle usb_device, uint pc, uint sp, uint delay_ms);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_exec(libusb_device_handle usb_device, uint addr);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_flash_erase(libusb_device_handle usb_device, uint addr, uint len);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_vector(libusb_device_handle usb_device, uint addr);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_write(libusb_device_handle usb_device, uint addr, byte* buffer, uint len);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_read(libusb_device_handle usb_device, uint addr, byte* buffer, uint len);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_poke(libusb_device_handle usb_device, uint addr, uint data);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_peek(libusb_device_handle usb_device, uint addr, uint* data);
    [DllImport("PicobootConnection.Native")] public static extern int picoboot_flash_id(libusb_device_handle usb_device, ulong* data);

    public static class Rp2350
    {
        [DllImport("PicobootConnection.Native")] public static extern int picoboot_reboot2(libusb_device_handle usb_device, picoboot_reboot2_cmd* reboot_cmd);
        [DllImport("PicobootConnection.Native")] public static extern int picoboot_get_info(libusb_device_handle usb_device, picoboot_get_info_cmd* cmd, byte* buffer, uint len);
        [DllImport("PicobootConnection.Native")] public static extern int picoboot_otp_write(libusb_device_handle usb_device, picoboot_otp_cmd* otp_cmd, byte* buffer, uint len);
        [DllImport("PicobootConnection.Native")] public static extern int picoboot_otp_read(libusb_device_handle usb_device, picoboot_otp_cmd* otp_cmd, byte* buffer, uint len);
    }

    [DllImport("PicobootConnection.Native")] public static extern memory_type PBC_get_memory_type(uint addr, model_t model);
    [DllImport("PicobootConnection.Native")] public static extern byte PBC_is_transfer_aligned(uint addr, model_t model);
    [DllImport("PicobootConnection.Native")] public static extern byte PBC_is_size_aligned(uint addr, int size);

    [SupportedOSPlatform("windows")]
    [DllImport("PicobootConnection.Native")]
    public static extern byte* PBC_GetUsbInstanceId(libusb_device device);
}
