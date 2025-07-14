libusb
===============================================================================

This directory contains the minimum files needed from the official Windows distribution of [libusb 1.0.27](https://github.com/libusb/libusb/releases/tag/v1.0.27). Only the binaries from `VS2022/MS64/dll` are included.

Note also `libusb_windows_common_minimal.h`, which exposes a small amount of information from libusb's internals to work around an API limitation. When updating libusb, double-check [that this PR wasn't merged](https://github.com/libusb/libusb/pull/537) (which would make this workaround redundant) and that nothing within libusb changed that might affect this file (particularly, the size of `libusb_device` and the layout of `winusb_device_priv`.) See `PBC_GetUsbInstanceId` for details.

libusb is licensed under LGPL 2.1, see [`COPYING`](COPYING) for details.
