cmake_minimum_required(VERSION 3.20)

project(QTHookLib LANGUAGES CXX)

add_library(QTHookLib SHARED main.cpp)

target_include_directories(QTHookLib PRIVATE
  ${CMAKE_SOURCE_DIR}/MinHook
  ${CMAKE_SOURCE_DIR}/MinHook/src
)

target_compile_definitions(QTHookLib PRIVATE UNICODE _UNICODE WIN32_LEAN_AND_MEAN)

target_link_libraries(QTHookLib PRIVATE minhook)

if(MSVC)
  target_compile_options(QTHookLib PRIVATE /W3)
endif()

# Name output dll to match original naming convention
if(CMAKE_SIZEOF_VOID_P EQUAL 8)
  set_target_properties(QTHookLib PROPERTIES OUTPUT_NAME QTHookLib64)
else()
  set_target_properties(QTHookLib PROPERTIES OUTPUT_NAME QTHookLib32)
endif()

