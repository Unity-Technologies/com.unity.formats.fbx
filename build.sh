#!/bin/bash

if [[ -e build ]]; then
    rm -rf build
fi

mkdir -p build
pushd build
cmake .. 
cmake --build . --target install
popd
