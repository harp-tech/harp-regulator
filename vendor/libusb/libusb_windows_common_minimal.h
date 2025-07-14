#pragma once
#include <stdint.h>

static_assert(sizeof(void*) == 8, "64-bit compilation only!");

extern "C"
{
    // From libusbi.h
    static inline void* usbi_get_device_priv(struct libusb_device* dev)
    {
        // 80 = PTR_ALIGN(sizeof(struct libusb_device))
        return (unsigned char*)dev + 80;
    }

    // From windows_common.h
    struct winusb_device_priv {
        bool initialized;
        bool root_hub;
        uint8_t active_config;
        uint8_t depth;
        const struct windows_usb_api_backend* apib;
        char* dev_id;
        // ... skipping remaining fields since we don't need them
    };
}
