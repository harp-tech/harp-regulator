#!/bin/bash -e

# Start in the directory containing this script
cd `dirname "${BASH_SOURCE[0]}"`

SOURCE_FOLDER=Native
PLATFORM_RID=`../../build/determine-rid.sh`
PICO_SDK_PATH=`realpath ../../vendor/pico-sdk`
BUILD_FOLDER_PREFIX=../../artifacts/obj/PicobootConnection.Native/cmake/$PLATFORM_RID

# Generate and build each configuration
function build_configuration() {
    if [[ -z $1 ]]; then
        echo "Missing configuration type"
        exit 1
    fi

    echo "============================================================================="
    echo "Generating makefile for $1 configuration..."
    echo "============================================================================="
    cmake \
        -G "Unix Makefiles" \
        -S $SOURCE_FOLDER \
        -B $BUILD_FOLDER_PREFIX-$1 \
        -DPICO_SDK_PATH=$PICO_SDK_PATH \
        -DCMAKE_BUILD_TYPE=$1

    echo "============================================================================="
    echo "Building $1 configuration..."
    echo "============================================================================="
    make --directory=$BUILD_FOLDER_PREFIX-$1 -j`nproc`
}

build_configuration Debug
build_configuration Release
