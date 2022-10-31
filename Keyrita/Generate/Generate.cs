using System;
using System.Collections.Generic;
using System.Diagnostics;
using Keyrita.Measurements;
using Keyrita.Operations;
using Keyrita.Operations.OperationUtil;
using Keyrita.Settings;
using Keyrita.Util;

namespace Keyrita.Generate
{
    /// <summary>
    /// Class responsible for controlling the generate algorithm.
    /// </summary>
    public class GenerateLayout
    {
        /// <summary>
        /// No constructor args, all obtained from the settings system.
        /// </summary>
        public GenerateLayout()
        {
        }

        /// <summary>
        /// The number of keys to fix at a time.
        /// </summary>
        const int OPTIMIZATION_DEPTH = 3;

        protected void SetupAnalysis()
        {
            // Make sure required nodes are installed. 
            AnalysisGraphSystem.InstallNode(eInputNodes.KeyLag);

            // Just do a quick analysis cycle to get our initial conditions.
            AnalysisGraphSystem.ResolveGraph();
            AnalysisGraphSystem.PreprocessSwapKeysResolveOrder();
            // NOTE: All future results will be obtained with the swap key functionality for max efficiency.
        }

        protected long MeasureLayoutsPerSecond()
        {
            long swapCount = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Perform a swap over and over again.
            while (true)
            {
                AnalysisGraphSystem.GenerateSignalSwapKeys(0, 0, 0, 1);
                swapCount++;

                if(sw.ElapsedMilliseconds > 5000)
                {
                    break;
                }
            }

            AnalysisGraphSystem.ResolveGraph();
            return swapCount / 5;
        }

        protected bool CheckStats(byte[][] expectedKbState, byte[][] kbStateResult, 
                                (int, int)[] expectedC2k, (int, int)[] c2k,
                                int[] expectedC2f, int[] c2f,
                                long expectedTotalSfbs, long totalSfbs,
                                long expectedTotalSfs, long totalSfs,
                                double[,] expectedSfbDistPerKey, double[,] sfbDistPerkey,
                                double[,] expectedSfsDistPerKey, double[,] sfsDistPerkey)

        {
            // Check the transformed kb state.
            if(!Utils.CompareDoubleArray(expectedKbState, kbStateResult))
            {
                LogUtils.Assert(false, "Transformed kb state should have returned back to the expected value after a double swap");
                return false;
            }

            // Check character to key results.
            if(!Utils.CompareArray(expectedC2k, c2k))
            {
                LogUtils.Assert(false, "Character to key state should have returned back to the expected value after a double swap");
                return false;
            }

            // Check character to finger results.
            if(!Utils.CompareArray(expectedC2f, c2f))
            {
                LogUtils.Assert(false, "Character to finger state should have returned back to the expected value after a double swap");
                return false;
            }

            // Two finger stats.
            if(expectedTotalSfbs != totalSfbs || expectedTotalSfs != totalSfs ||
                !Utils.CompareRectArrayDoubles(expectedSfbDistPerKey, sfbDistPerkey) ||
                !Utils.CompareRectArrayDoubles(expectedSfsDistPerKey, sfsDistPerkey))
            {
                LogUtils.Assert(false, "Same finger stats should have been preserved");
                return false;
            }

            return true;
        }

        protected bool TestSwaps()
        {
            // Values to compare against.
            var kbStateResult = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            var tfs = (TwoFingerStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TwoFingerStats];
            var c2kResult = (TransformedCharacterToKeyResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToKey];
            var c2fResult = (TransformedCharacterToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];
            var scissorsResult = (ScissorsResult)AnalysisGraphSystem.ResolvedNodes[eMeasurements.Scissors];

            var expectedKbState = Utils.CopyDoubleArray(kbStateResult.TransformedKbState);
            var expectedC2k = Utils.CopyArray(c2kResult.CharacterToKey);
            var expectedC2f = Utils.CopyArray(c2fResult.CharacterToFinger);
            var expectedSfbDistPerKey = Utils.CopyRectArray(tfs.SfbDistancePerKey);
            var expectedSfsDistPerKey = Utils.CopyRectArray(tfs.SfsDistancePerKey);
            var expectedTotalSfbs = tfs.TotalSfbs;
            var expectedTotalSfs = tfs.TotalSfs;
            var expectedTotalScissors = scissorsResult.TotalResult;

            Random rand = new Random();
            int swapCount = 5000;

            LogUtils.LogInfo("Starting cached swapped tests.");
            Stopwatch sw = new Stopwatch();

            // For each key, swap it with every other key, then swap back, and make sure we have the same results.
            for (int i = 0; i < swapCount; i++)
            {
                int k1i = rand.Next(0, KeyboardStateSetting.ROWS);
                int k1j = rand.Next(0, KeyboardStateSetting.COLS);
                int k2i = rand.Next(0, KeyboardStateSetting.ROWS);
                int k2j = rand.Next(0, KeyboardStateSetting.COLS);

                // It never makes sense to swap a key with itself, wont even bother to make sure that behaves properly.
                if (k1i == k2i && k1j == k2j) continue;

                AnalysisGraphSystem.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);
                AnalysisGraphSystem.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);

                if (!CheckStats(expectedKbState, kbStateResult.TransformedKbState, expectedC2k, c2kResult.CharacterToKey,
                    expectedC2f, c2fResult.CharacterToFinger, expectedTotalSfbs, tfs.TotalSfbs, expectedTotalSfs, tfs.TotalSfs,
                    expectedSfbDistPerKey, tfs.SfbDistancePerKey, expectedSfsDistPerKey, tfs.SfsDistancePerKey))
                {
                    return false;
                }
            }

            sw.Start();
            // Now perform completely random swaps, but this time don't swap back!
            // At the end we will swap the keyboard back to its initial position and compare to the expected results.
            for (int i = 0; i < swapCount; i++)
            {
                int k1i = rand.Next(0, KeyboardStateSetting.ROWS);
                int k1j = rand.Next(0, KeyboardStateSetting.COLS);

                int k2i = rand.Next(0, KeyboardStateSetting.ROWS);
                int k2j = rand.Next(0, KeyboardStateSetting.COLS);

                // It never makes sense to swap a key with itself, wont even bother to make sure that behaves properly.
                if (k1i == k2i && k1j == k2j) continue;

                AnalysisGraphSystem.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);
            }

            // Now swap every key that's out of place with its correct key to get back to the initial keyboard state.
            // For this to work, we have to assume at a minimum the keyboard state swapping is working properly.
            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    if (expectedKbState[i][j] != kbStateResult.TransformedKbState[i][j])
                    {
                        // Find the key to swap with and perform the swap. Note the key can only be in front.
                        for(int k = 0; k < KeyboardStateSetting.ROWS; k++)
                        {
                            for(int w = 0; w < KeyboardStateSetting.COLS; w++)
                            {
                                if (kbStateResult.TransformedKbState[k][w] == expectedKbState[i][j])
                                {
                                    // Swap with this key, and if the swapping is correct, were good to go.
                                    AnalysisGraphSystem.GenerateSignalSwapKeys(i, j, k, w);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (!CheckStats(expectedKbState, kbStateResult.TransformedKbState, expectedC2k, c2kResult.CharacterToKey,
                expectedC2f, c2fResult.CharacterToFinger, expectedTotalSfbs, tfs.TotalSfbs, expectedTotalSfs, tfs.TotalSfs,
                expectedSfbDistPerKey, tfs.SfbDistancePerKey, expectedSfsDistPerKey, tfs.SfsDistancePerKey))
            {
                return false;
            }

            sw.Stop();
            LogUtils.LogInfo($"Finished cached swapped tests. ({sw.ElapsedMilliseconds} ms)");

            return false;
        }

        /// <summary>
        /// Given the current layout, locked keys, and much more, find the best possible arrangement.
        /// When finished, set the screen to use the new layout.
        /// This class needs to hijack the operator system. (nothing else can do analysis in the meantime). 
        /// </summary>
        public void GenerateBetterLayout()
        {
            SetupAnalysis();

            // Only proceed if the cache swapping system isn't obviously broken.
            if (TestSwaps())
            {

            }

            //long swapCount = MeasureLayoutsPerSecond();
            //LogUtils.LogInfo($"Swaps per second: {swapCount}");
        }
    }
}
