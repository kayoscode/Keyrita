#include "pch.h"
#include <iostream>
#include <map>
#include <vector>

#define GET_BG(x, y) ((x * numValidChars) + y)
#define GET_TG(x, y) ((x * numValidChars) + y)

/// <summary>
/// Create an enumeration of all keys on the layout which will be pressed with the same finger.
/// </summary>
/// <param name="keyToFinger"></param>
static void CreateSameFingerMappings(int* keyToFinger, std::vector<std::tuple<int, int, int>>& sameFingerKeys) {
    // We want to iterate through each key and determine which finger is used for it. Using that, create an enumeration of all keys which must use the same finger.
    // This doesn't include keys which are identical.
    // Were enumerating the values like this because in the future, it will make sense to reuse
    for (int i = 0; i < 30; i++) {
        for (int j = 0; j < 30; j++) {
            if (i == j) {
                continue;
            }

            if (keyToFinger[i] == keyToFinger[j]) {
                sameFingerKeys.push_back(std::make_tuple(i, j, keyToFinger[i]));
            }
        }
    }
}

long long CalculateTotalSFBs(char* keyboardState, unsigned int* bigramFreq, std::vector<std::tuple<int, int, int>>& sameFingerKeys,
    int numValidChars, long long* perFingerResults) {
    long long totalSfbs = 0;

    for (std::tuple<int, int, int> sfk : sameFingerKeys) {
        int idx1 = std::get<0>(sfk);
        int idx2 = std::get<1>(sfk);

        int k1 = keyboardState[idx1];
        int k2 = keyboardState[idx2];

        int addedSfbs = bigramFreq[GET_BG(k1, k2)];
        totalSfbs += addedSfbs;
        perFingerResults[std::get<2>(sfk)] += addedSfbs;
    }

    return totalSfbs;
}

long long CalculateTotalSFSs(char* keyboardState, unsigned int* trigramFreq, 
    std::vector<std::tuple<int, int, int>>& sameFingerkeys,
    int numValidchars, long long* perFingerResults) 
{
    return 0;
}

extern "C" {
/// <summary>
/// Measures the total number of sfbs in the layout.
/// </summary>
/// <param name="keyboardState"></param>
/// <param name="bigramFreq"></param>
/// <returns></returns>
__declspec(dllexport) long long __cdecl MeasureTotalSFBs(char* keyboardState,
    unsigned int* bigramFreq,
    int* keyToFinger,
    int numValidChars,
    long long* perFingerResults) 
{
    std::vector<std::tuple<int, int, int>> sameFingerKeys;
    CreateSameFingerMappings(keyToFinger, sameFingerKeys);

    return CalculateTotalSFBs(keyboardState, bigramFreq, sameFingerKeys, numValidChars, perFingerResults);
}

/// <summary>
/// Measures the total number trigrams which start and end on the same finger.
/// </summary>
/// <param name="keyboardState"></param>
/// <param name="trigramFreq"></param>
/// <param name="keyToFinger"></param>
/// <param name="numValidChars"></param>
/// <param name="perFingerResults"></param>
/// <returns></returns>
__declspec(dllexport) long long __cdecl MeasureTotalSFSs(char* keyboardState,
    unsigned int* trigramFreq,
    int* keyToFinger,
    int numValidChars,
    long long* perFingerResults) 
{
    std::vector<std::tuple<int, int, int>> sameFingerKeys;
    CreateSameFingerMappings(keyToFinger, sameFingerKeys);
    return 0;
}

/// <summary>
/// Measures the total number trigrams which start and end on the same finger.
/// </summary>
/// <param name="keyboardState"></param>
/// <param name="trigramFreq"></param>
/// <param name="keyToFinger"></param>
/// <param name="numValidChars"></param>
/// <param name="perFingerResults"></param>
/// <returns></returns>
__declspec(dllexport) long long __cdecl MeasureTotalSFSs(char* keyboardState,
    unsigned int* trigramFreq,
    int* keyToFinger,
    int numValidChars,
    long long* perFingerResults) 
{
    std::vector<std::tuple<int, int, int>> sameFingerKeys;
    CreateSameFingerMappings(keyToFinger, sameFingerKeys);
    return 0;
}
}
