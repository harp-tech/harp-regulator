# Harp Regulator

> **Regulator** (musical)
>
> Noun. A person who maintains and tunes ("regulates") a harp.

⚠ This tool is in alpha and relies on Harp specifications which have not been ratified. It will work for [Pico-based Harp devices](https://github.com/harp-tech/core.pico) in manual update mode, but most functionality requires special non-conforming firmware. See [Harp Toolkit](https://github.com/harp-tech/harp-cli) for the existing firmware update tool.

See the docs folder for micro-specifications that may need to be ratified as part of the Harp spec before this tool can be productized.

## Building

Ensure the dependencies listed below for your platform are installed and run `dotnet build` in the root.

### Windows

| Depdency | Version known to work |
|----------|-----------------------|
| [Visual Studio](https://visualstudio.microsoft.com/vs/) w/ C++ workload | 17.14.7 |
| [.NET 8 SDK](https://dot.net/) | 8.0.411 |
| [CMake](https://cmake.org/) | 3.30.2 |

### Linux

Linux support is not well-tested and may require refinement. Building is known to work on Ubuntu 24.04 Noble x64

| Depdency | Version known to work |
|----------|-----------------------|
| `build-essential` | 12.10ubuntu1 |
| `libusb-1.0-0-dev` | 2:1.0.27-1 |
| `dotnet-sdk-8.0` | 8.0.116-0ubuntu1~24.04.1 |
