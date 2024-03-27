@echo off

if exist build (
    rd /s /q build
)
md build
cd build
REM Explicitly specify the target platform as "x64". Otherwise it fails on ARM64 Windows which has x86 VS2022 installed.
REM This build script doesn't really build anything, it just installs Max/Maya scripts and CHANGELOG.md/LICENSE.md into the package. So the target platform actually doesn't matter.
cmake .. -Ax64
cmake --build . --target install
cd ..
