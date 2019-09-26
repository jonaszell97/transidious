cmake_minimum_required(VERSION 3.9)
project(UnityTriangle)

set(SOURCE_FILES UnityTriangle/triangle.cpp)
set(CMAKE_CXX_VISIBILITY_PRESET default)
set(CMAKE_VISIBILITY_INLINES_HIDDEN 0)

add_library(unitytriangle SHARED ${SOURCE_FILES})
set(CMAKE_EXE_LINKER_FLAGS " -static")
target_link_libraries(unitytriangle -static-libgcc -static-libstdc++)