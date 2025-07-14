namespace PicobootConnection;

public enum picoboot_device_result
{
    dr_vidpid_bootrom_ok,
    dr_vidpid_bootrom_no_interface,
    dr_vidpid_bootrom_cant_connect,
    dr_vidpid_micropython,
    dr_vidpid_picoprobe,
    dr_vidpid_unknown,
    dr_error,
    dr_vidpid_stdio_usb,
    dr_vidpid_stdio_usb_cant_connect,
}
