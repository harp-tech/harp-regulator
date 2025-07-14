@echo off
setlocal enabledelayedexpansion

:: Start in the directory containing this script
cd %~dp0

set SOURCE_FOLDER=Native
set PICO_SDK_PATH=%~dp0/../../vendor/pico-sdk/
set LIBUSB_INCLUDE_DIR=%~dp0/../../vendor/libusb/
set LIBUSB_LIB_DIR=%~dp0/../../vendor/libusb/

:: Determine platform RID and build folder
call ..\..\build\determine-rid.cmd || exit /B !ERRORLEVEL!
set BUILD_FOLDER=..\..\artifacts\obj\PicobootConnection.Native\cmake\%PLATFORM_RID%

:: Ensure build folder is protected from Directory.Build.* influences
if not exist %BUILD_FOLDER% (
    mkdir %BUILD_FOLDER%
    echo ^<Project^>^</Project^> > %BUILD_FOLDER%/Directory.Build.props
    echo ^<Project^>^</Project^> > %BUILD_FOLDER%/Directory.Build.targets
    echo # > %BUILD_FOLDER%/Directory.Build.rsp
)

:: (Re)generate the Visual Studio solution and build in all configurations
:: We don't specify a generator specifically so that CMake will default to the latest installed verison of Visual Studio
:: https://github.com/Kitware/CMake/blob/0c038689be424ca71a6699a993adde3bcaa15b6c/Source/cmake.cxx#L2213-L2214
:: C5287 is disabled below because code picoboot_connection.c triggers it.
cmake ^
    -S Native ^
    -B %BUILD_FOLDER% ^
    -DPICO_SDK_PATH=%PICO_SDK_PATH% ^
    -DLIBUSB_INCLUDE_DIR=%LIBUSB_INCLUDE_DIR% ^
    -DLIBUSB_LIBRARIES=%LIBUSB_LIB_DIR%/libusb-1.0.lib ^
    -DCMAKE_CXX_FLAGS=/wd5287 ^
    || exit /B 1

echo ==============================================================================
echo Building PicobootConnection.Native %PLATFORM_RID% debug build...
echo ==============================================================================
cmake --build %BUILD_FOLDER% --config Debug || exit /B 1

echo ==============================================================================
echo Building PicobootConnection.Native %PLATFORM_RID% release build...
echo ==============================================================================
cmake --build %BUILD_FOLDER% --config Release || exit /B 1
