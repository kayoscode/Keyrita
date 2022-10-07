#include "pch.h"
#include <iostream>

extern "C"
{
   __declspec(dllexport) void TestFromDLL()
   {
      std::cout << "Test";
   }
}