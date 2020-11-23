cmake_minimum_required(VERSION 3.15)
project(TriangleAPI)

set(CMAKE_CXX_STANDARD 17)

# find LLVM
find_package(LLVM 10 REQUIRED CONFIG)

# llvm libraries
llvm_map_components_to_libnames(llvm_libs support)
string(REPLACE ";LLVM;" ";" llvm_libs "${llvm_libs}")
string(REPLACE ";LLVM-C" "" llvm_libs "${llvm_libs}")
include_directories(${LLVM_INCLUDE_DIRS})

message("Found LLVM Libs: ${llvm_libs}")

add_executable(TriangleAPI main.cpp)
target_link_libraries(TriangleAPI ${llvm_libs})