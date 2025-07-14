cmake_minimum_required(VERSION 3.13)
project(PicobootConnection.Native)

add_library(PicobootConnection.Native SHARED
    PicobootConnection.cpp
    picoboot_connection.c
    picoboot_connection.h
)

list(APPEND CMAKE_MODULE_PATH ${CMAKE_CURRENT_LIST_DIR}/cmake)
find_package(LIBUSB REQUIRED)

include(${PICO_SDK_PATH}/pico_sdk_version.cmake)
add_subdirectory(${PICO_SDK_PATH}/src/common/boot_picoboot_headers boot_picoboot_headers)
add_subdirectory(${PICO_SDK_PATH}/src/host/pico_platform pico_platform)
add_subdirectory(${PICO_SDK_PATH}/src/rp2_common/boot_bootrom_headers boot_bootrom_headers)
add_subdirectory(${PICO_SDK_PATH}/src/common/pico_usb_reset_interface_headers pico_usb_reset_interface_headers)

target_include_directories(PicobootConnection.Native PRIVATE ${PICO_SDK_PATH}/src/rp2_common/pico_stdio_usb/include)

target_include_directories(PicobootConnection.Native PRIVATE ${LIBUSB_INCLUDE_DIR})
target_compile_definitions(PicobootConnection.Native PRIVATE HAS_LIBUSB=1)
target_link_libraries(PicobootConnection.Native
    ${LIBUSB_LIBRARIES}
    boot_picoboot_headers
    pico_platform_headers
    boot_bootrom_headers
    pico_usb_reset_interface_headers
)
