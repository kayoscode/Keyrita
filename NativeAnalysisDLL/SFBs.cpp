#include "pch.h"
#include <iostream>
#include <map>
#include <vector>

#define DAIDX(x, y) ((x * 30) + y)

/// <summary>
/// Create an enumeration of all keys on the layout which will be pressed with the same finger.
/// </summary>
/// <param name="keyToFinger"></param>
static void CreateSameFingerMappings(int* keyToFinger, std::vector<std::tuple<int, int>>& sameFingerKeys) {
   // We want to iterate through each key and determine which finger is used for it. Using that, create an enumeration of all keys which must use the same finger.
   // This doesn't include keys which are identical.
   // Were enumerating the values like this because in the future, it will make sense to reuse 
   for (int i = 0; i < 30; i++) {
      for (int j = 0; j < 30; j++) {
         if (i == j) {
            continue;
         }

         if (keyToFinger[i] == keyToFinger[j]) {
            sameFingerKeys.push_back(std::make_tuple(i, j));
         }
      }
   }
}

long long CalculateTotalSFBs(char* keyboardState, char* bigramFreq, std::vector<std::tuple<int, int>>& sameFingerKeys) {
   long long totalSfbs = 0;

   for (std::tuple<int, int> sfk : sameFingerKeys) {
      int idx1 = std::get<0>(sfk);
      int idx2 = std::get<1>(sfk);

      totalSfbs += bigramFreq[DAIDX(idx1, idx2)];
   }

   return totalSfbs;
}

extern "C" {

/// <summary>
/// Measures the total number of sfbs in the layout.
/// </summary>
/// <param name="keyboardState"></param>
/// <param name="bigramFreq"></param>
/// <returns></returns>
__declspec(dllexport) long long __cdecl MeasureTotalSFBs(char* keyboardState, char* bigramFreq, int* keyToFinger) {
   std::vector<std::tuple<int, int>> sameFingerKeys;
   CreateSameFingerMappings(keyToFinger, sameFingerKeys);

   return CalculateTotalSFBs(keyboardState, bigramFreq, sameFingerKeys);
}
}
