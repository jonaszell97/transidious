# CMAKE generated file: DO NOT EDIT!
# Generated by "Unix Makefiles" Generator, CMake Version 3.10

# Delete rule output on recipe failure.
.DELETE_ON_ERROR:


#=============================================================================
# Special targets provided by cmake.

# Disable implicit rules so canonical targets will work.
.SUFFIXES:


# Remove some rules from gmake that .SUFFIXES does not remove.
SUFFIXES =

.SUFFIXES: .hpux_make_needs_suffix_list


# Suppress display of executed commands.
$(VERBOSE).SILENT:


# A target that is always out of date.
cmake_force:

.PHONY : cmake_force

#=============================================================================
# Set environment variables for the build.

# The shell in which to execute make rules.
SHELL = /bin/sh

# The CMake executable.
CMAKE_COMMAND = /usr/bin/cmake

# The command to remove a file.
RM = /usr/bin/cmake -E remove -f

# Escaping for special characters.
EQUALS = =

# The top-level source directory on which CMake was run.
CMAKE_SOURCE_DIR = /home/jonas/transidious/TblGen

# The top-level build directory on which CMake was run.
CMAKE_BINARY_DIR = /home/jonas/transidious/TblGen

# Include any dependencies generated for this target.
include CMakeFiles/transidious-tblgens.dir/depend.make

# Include the progress variables for this target.
include CMakeFiles/transidious-tblgens.dir/progress.make

# Include the compile flags for this target's objects.
include CMakeFiles/transidious-tblgens.dir/flags.make

CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o: CMakeFiles/transidious-tblgens.dir/flags.make
CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o: Backends/OSMImportBackend.cpp
	@$(CMAKE_COMMAND) -E cmake_echo_color --switch=$(COLOR) --green --progress-dir=/home/jonas/transidious/TblGen/CMakeFiles --progress-num=$(CMAKE_PROGRESS_1) "Building CXX object CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o"
	/usr/bin/clang++  $(CXX_DEFINES) $(CXX_INCLUDES) $(CXX_FLAGS) -o CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o -c /home/jonas/transidious/TblGen/Backends/OSMImportBackend.cpp

CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.i: cmake_force
	@$(CMAKE_COMMAND) -E cmake_echo_color --switch=$(COLOR) --green "Preprocessing CXX source to CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.i"
	/usr/bin/clang++ $(CXX_DEFINES) $(CXX_INCLUDES) $(CXX_FLAGS) -E /home/jonas/transidious/TblGen/Backends/OSMImportBackend.cpp > CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.i

CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.s: cmake_force
	@$(CMAKE_COMMAND) -E cmake_echo_color --switch=$(COLOR) --green "Compiling CXX source to assembly CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.s"
	/usr/bin/clang++ $(CXX_DEFINES) $(CXX_INCLUDES) $(CXX_FLAGS) -S /home/jonas/transidious/TblGen/Backends/OSMImportBackend.cpp -o CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.s

CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o.requires:

.PHONY : CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o.requires

CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o.provides: CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o.requires
	$(MAKE) -f CMakeFiles/transidious-tblgens.dir/build.make CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o.provides.build
.PHONY : CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o.provides

CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o.provides.build: CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o


# Object files for target transidious-tblgens
transidious__tblgens_OBJECTS = \
"CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o"

# External object files for target transidious-tblgens
transidious__tblgens_EXTERNAL_OBJECTS =

libtransidious-tblgens.so: CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o
libtransidious-tblgens.so: CMakeFiles/transidious-tblgens.dir/build.make
libtransidious-tblgens.so: /usr/lib/llvm-6.0/lib/libLLVMSupport.a
libtransidious-tblgens.so: /usr/lib/llvm-6.0/lib/libLLVMDemangle.a
libtransidious-tblgens.so: CMakeFiles/transidious-tblgens.dir/link.txt
	@$(CMAKE_COMMAND) -E cmake_echo_color --switch=$(COLOR) --green --bold --progress-dir=/home/jonas/transidious/TblGen/CMakeFiles --progress-num=$(CMAKE_PROGRESS_2) "Linking CXX shared library libtransidious-tblgens.so"
	$(CMAKE_COMMAND) -E cmake_link_script CMakeFiles/transidious-tblgens.dir/link.txt --verbose=$(VERBOSE)

# Rule to build all files generated by this target.
CMakeFiles/transidious-tblgens.dir/build: libtransidious-tblgens.so

.PHONY : CMakeFiles/transidious-tblgens.dir/build

CMakeFiles/transidious-tblgens.dir/requires: CMakeFiles/transidious-tblgens.dir/Backends/OSMImportBackend.cpp.o.requires

.PHONY : CMakeFiles/transidious-tblgens.dir/requires

CMakeFiles/transidious-tblgens.dir/clean:
	$(CMAKE_COMMAND) -P CMakeFiles/transidious-tblgens.dir/cmake_clean.cmake
.PHONY : CMakeFiles/transidious-tblgens.dir/clean

CMakeFiles/transidious-tblgens.dir/depend:
	cd /home/jonas/transidious/TblGen && $(CMAKE_COMMAND) -E cmake_depends "Unix Makefiles" /home/jonas/transidious/TblGen /home/jonas/transidious/TblGen /home/jonas/transidious/TblGen /home/jonas/transidious/TblGen /home/jonas/transidious/TblGen/CMakeFiles/transidious-tblgens.dir/DependInfo.cmake --color=$(COLOR)
.PHONY : CMakeFiles/transidious-tblgens.dir/depend

