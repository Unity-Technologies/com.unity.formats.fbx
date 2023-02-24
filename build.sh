#!/bin/bash

## Any subsequent(*) commands which fail will cause the shell script to exit immediately
set -e

if [[ -e build ]]; then
    rm -rf build
fi

mkdir -p build
pushd build
cmake .. 
cmake --build . --target install
popd
