@echo off

if exist build (
    rd /s /q build
)
md build
cd build
cmake ..
cmake --build . --target install
cd ..
