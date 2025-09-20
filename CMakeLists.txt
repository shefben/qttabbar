cmake_minimum_required(VERSION 3.20)

project(QTTabBarRebirth LANGUAGES CXX)

option(BUILD_NATIVE_ONLY "Build only native hook libraries" OFF)
option(BUILD_QTTABBAR_CSHARP "Also build the C# QTTabBar project using MSBuild" ON)

# Native components
add_subdirectory(MinHook)
add_subdirectory(QTHookLib)

if(BUILD_QTTABBAR_CSHARP AND NOT BUILD_NATIVE_ONLY)
  # Build the C# project via MSBuild from CMake. You must provide MSBuild path.
  set(MSBUILD_EXE "" CACHE FILEPATH "Path to MSBuild.exe (e.g. C:/Program Files/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe)")
  if(NOT EXISTS "${MSBUILD_EXE}")
    message(WARNING "MSBUILD_EXE not set or invalid. Skipping C# build. Set -DMSBUILD_EXE=... to enable.")
  else()
    # Build only QTTabBar C# project (skip installers). Requires .NET Framework 3.5 targeting pack.
    add_custom_target(QTTabBar_CS ALL
      COMMAND "${MSBUILD_EXE}" "${CMAKE_SOURCE_DIR}/QTTabBar/QTTabBar.csproj" /m /p:Configuration=Release
      WORKING_DIRECTORY "${CMAKE_SOURCE_DIR}"
      COMMENT "Building C# QTTabBar with MSBuild"
      VERBATIM)
  endif()
endif()

