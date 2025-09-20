cmake_minimum_required(VERSION 3.20)

project(minhook LANGUAGES C)

file(GLOB MINHOOK_SRC
  "src/*.c" "src/*.cpp"
  "src/HDE32/*.c"
  "src/HDE64/src/*.c"
)

add_library(minhook STATIC ${MINHOOK_SRC})
target_include_directories(minhook PUBLIC
  ${CMAKE_CURRENT_SOURCE_DIR}
  ${CMAKE_CURRENT_SOURCE_DIR}/src
  ${CMAKE_CURRENT_SOURCE_DIR}/src/HDE32
  ${CMAKE_CURRENT_SOURCE_DIR}/src/HDE64/include
)

if(MSVC)
  target_compile_definitions(minhook PRIVATE _CRT_SECURE_NO_WARNINGS WIN32_LEAN_AND_MEAN)
endif()

