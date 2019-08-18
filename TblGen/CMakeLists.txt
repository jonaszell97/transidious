

cmake_minimum_required(VERSION 3.10)
project(TransidiousTblgens)

set(CMAKE_CXX_COMPILER /usr/bin/clang++)
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fno-exceptions -Wall")

# include tblgen
include_directories("$ENV{TBLGEN_PATH}/include")

# find LLVM
set(LLVM_DIR /usr/local/Cellar/llvm/6.0.0/lib/cmake/llvm)
find_package(LLVM REQUIRED CONFIG)

# llvm libraries
llvm_map_components_to_libnames(llvm_libs support)

message(STATUS "Found LLVM ${LLVM_PACKAGE_VERSION}")
message(STATUS "Found LLVM libs ${llvm_libs}")
message(STATUS "Using LLVMConfig.cmake in: ${LLVM_DIR}")

include_directories(${LLVM_INCLUDE_DIRS})
add_definitions(${LLVM_DEFINITIONS})

add_library(transidious-tblgens SHARED Backends/OSMImportBackend.cpp)
target_link_libraries(transidious-tblgens ${llvm_libs})

