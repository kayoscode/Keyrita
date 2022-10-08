#include "pch.h"
#include <iostream>
#include <map>

extern "C" {
	std::map<wchar_t, wchar_t> ToLowerSpecialCases{
		{ ':', ';' },
		{ '"', '\'' },
		{ '<', ',' },
		{ '>', '.' },
		{ '?', '/' },
	};

	#define DAIDX(y, x) ((charToIndex[filteredString[y]] * charsetSize) + charToIndex[filteredString[x]])
	#define TAIDX(z, y, x) ((charToIndex[filteredString[z]] * (charsetSize * charsetSize)) + DAIDX(y, x))

	#define SKIDX(z, y, x) ((z * (charsetSize * charsetSize)) + DAIDX(y, x))

	/// <summary>
	/// Using a string to reprepsent the input dataset,
	/// and a valid character set, analyze the frequencies of each
	/// letter, bigram, and trigram.
	/// The arrays should be initialized to the correct sizes.
	/// dimensions of size validCharset.size()
	/// The first dimension of the skipgram array should always be 3,
	/// then after that the same dimensions as the bigram chart.
	/// </summary>
	/// <param name="string"></param>
	/// <param name="validCharset"></param>
	/// <param name="charFreq"></param>
	/// <param name="bigramFreq"></param>
	/// <param name="trigramFreq"></param>
	__declspec(dllexport) long long __cdecl AnalyzeDataset(wchar_t* dataset, int datasetSize,
		wchar_t* validCharset, int charsetSize,
		unsigned int* charFreq,
		unsigned int* bigramFreq,
		unsigned int* trigramFreq,
		unsigned int* skipGramFreq,
		double* progress) 
	{
        *progress = 0;

		// Need a map to convert each character to lower. Add special cases here.
		wchar_t* toLowerMap = new wchar_t[WCHAR_MAX + 1];
		for (wchar_t i = 0; i < WCHAR_MAX; i++) {
			auto special = ToLowerSpecialCases.find(i);

			if (special != ToLowerSpecialCases.end()) {
				toLowerMap[i] = special->second;
			}
			else if (i <= 255) {
				toLowerMap[i] = tolower(i);
			}
			else {
				toLowerMap[i] = i;
			}
		}

		// Really it should be much much greater than 100, but I don't really care. Just need it to be more than 4
		if (charsetSize == 0 || datasetSize < 6) {
			return 0;
		}

		// To save time, only initialize the characters we actually care about.
		unsigned int* charToIndex = new unsigned int[WCHAR_MAX + 1];
		bool* charValid = new bool[WCHAR_MAX + 1];

		std::memset(charToIndex, 0, sizeof(int) * WCHAR_MAX + 1);
		std::memset(charValid, false, sizeof(bool) * WCHAR_MAX + 1);

		for (int i = 0; i < charsetSize; i++) {
			charToIndex[validCharset[i]] = i;
			charValid[validCharset[i]] = true;
		}

		// Filter input string -> remove all special characters, and transform to lower case.
		std::wstring filteredString;
		for (int i = 0; i < datasetSize; i++) {
			char newC = toLowerMap[dataset[i]];

			if (charValid[newC]) {
				filteredString += newC;
			}
		}

		// Deal with the first few characters to avoid extra logic in the big loop.
		charFreq[charToIndex[filteredString[0]]]++;
		charFreq[charToIndex[filteredString[1]]]++;
		charFreq[charToIndex[filteredString[2]]]++;
		charFreq[charToIndex[filteredString[3]]]++;
		charFreq[charToIndex[filteredString[4]]]++;
		charFreq[charToIndex[filteredString[5]]]++;
		bigramFreq[DAIDX(0, 1)]++;
		bigramFreq[DAIDX(1, 2)]++;
		bigramFreq[DAIDX(2, 3)]++;
		bigramFreq[DAIDX(3, 4)]++;
		bigramFreq[DAIDX(4, 5)]++;

		trigramFreq[TAIDX(0, 1, 2)]++;
		trigramFreq[TAIDX(1, 2, 3)]++;
		trigramFreq[TAIDX(2, 3, 4)]++;
		trigramFreq[TAIDX(3, 4, 5)]++;

		// SkipGrams of [2]. (meaning two characters between).
		skipGramFreq[SKIDX(0, 0, 3)]++;
		skipGramFreq[SKIDX(0, 1, 4)]++;
		skipGramFreq[SKIDX(0, 2, 5)]++;

		// SkipGrams of [3]
		skipGramFreq[SKIDX(1, 0, 4)]++;
		skipGramFreq[SKIDX(1, 1, 5)]++;

		// SkipGrams of [4]
		skipGramFreq[SKIDX(2, 0, 5)]++;

		// The dataset is big enough to just skip the first couple characters, no problem.
		for (unsigned int i = 6; i < filteredString.size(); i++) {
			charFreq[charToIndex[filteredString[i]]]++;

			// Deal with bigrams.
			bigramFreq[DAIDX(i - 1, i)]++;

			// Deal with trigrams.
			trigramFreq[TAIDX(i - 2, i - 1, i)]++;

			// Deal with skipgrams.
			skipGramFreq[SKIDX(0, i - 3, i)]++;
			skipGramFreq[SKIDX(1, i - 4, i)]++;
			skipGramFreq[SKIDX(2, i - 5, i)]++;

			*progress = (((double)i + 1) / (double)filteredString.size()) * 100.0;
		}

		delete[] toLowerMap;
		delete[] charValid;
		delete[] charToIndex;

		return filteredString.size();
	}
}