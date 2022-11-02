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
                AnalysisGraphSystem.GenerateSignalSwapKeys(0, 4, 0, 5);
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
                                double[,] expectedSfsDistPerKey, double[,] sfsDistPerkey,
                                long expectedTotalScissors, long totalScissors,
                                long[,] expectedPerKeyScissors, long[,] perKeyScissors,
                                double[,] expectedKeyLag, double[,] keyLag)

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

            // Check scissors.
            if(expectedTotalScissors != totalScissors ||
                !Utils.CompareRectArray(expectedPerKeyScissors, perKeyScissors))
            {
                LogUtils.Assert(false, "Scissors should have returned back to the expected value after a double swap");
                return false;
            }

            // Check key speed.
            if(!Utils.CompareRectArrayDoubles(expectedKeyLag, keyLag))
            {
                LogUtils.Assert(false, "Key lag should have returned back to the expected value after a double swap");
                return false;
            }

            return true;
        }

        private Random mRand = new Random();

        protected bool TestSwaps()
        {
            // Values to compare against.
            var kbStateResult = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            var tfs = (TwoFingerStatsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TwoFingerStats];
            var c2kResult = (TransformedCharacterToKeyResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToKey];
            var c2fResult = (TransformedCharacterToFingerAsIntResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];
            var scissorsResult = (ScissorsResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.ScissorsIntermediate];
            var keyLagResult = (KeyLagResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyLag];

            var expectedKbState = Utils.CopyDoubleArray(kbStateResult.TransformedKbState);
            var expectedC2k = Utils.CopyArray(c2kResult.CharacterToKey);
            var expectedC2f = Utils.CopyArray(c2fResult.CharacterToFinger);
            var expectedSfbDistPerKey = Utils.CopyRectArray(tfs.SfbDistancePerKey);
            var expectedSfsDistPerKey = Utils.CopyRectArray(tfs.SfsDistancePerKey);
            var expectedTotalSfbs = tfs.TotalSfbs;
            var expectedTotalSfs = tfs.TotalSfs;
            var expectedTotalScissors = scissorsResult.TotalResult;
            var expectedScissorsPerKey = Utils.CopyRectArray(scissorsResult.PerKeyResult);
            var expectedKeyLag = Utils.CopyRectArray(keyLagResult.PerKeyResult);

            int swapCount = 5000;

            LogUtils.LogInfo("Starting key swap tests.");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // For each key, swap it with every other key, then swap back, and make sure we have the same results.
            for (int i = 0; i < swapCount; i++)
            {
                int k1i = mRand.Next(0, KeyboardStateSetting.ROWS);
                int k1j = mRand.Next(0, KeyboardStateSetting.COLS);
                int k2i = mRand.Next(0, KeyboardStateSetting.ROWS);
                int k2j = mRand.Next(0, KeyboardStateSetting.COLS);

                // It never makes sense to swap a key with itself, wont even bother to make sure that behaves properly.
                if (k1i == k2i && k1j == k2j) continue;

                AnalysisGraphSystem.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);
                AnalysisGraphSystem.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);

                if (!CheckStats(expectedKbState, kbStateResult.TransformedKbState, expectedC2k, c2kResult.CharacterToKey,
                    expectedC2f, c2fResult.CharacterToFinger, expectedTotalSfbs, tfs.TotalSfbs, expectedTotalSfs, tfs.TotalSfs,
                    expectedSfbDistPerKey, tfs.SfbDistancePerKey, expectedSfsDistPerKey, tfs.SfsDistancePerKey,
                    expectedTotalScissors, scissorsResult.TotalResult,
                    expectedScissorsPerKey, scissorsResult.PerKeyResult,
                    expectedKeyLag, keyLagResult.PerKeyResult))
                {
                    return false;
                }
            }

            // Now perform completely random swaps, but this time don't swap back!
            // At the end we will swap the keyboard back to its initial position and compare to the expected results.
            for (int i = 0; i < swapCount; i++)
            {
                int k1i = mRand.Next(0, KeyboardStateSetting.ROWS);
                int k1j = mRand.Next(0, KeyboardStateSetting.COLS);

                int k2i = mRand.Next(0, KeyboardStateSetting.ROWS);
                int k2j = mRand.Next(0, KeyboardStateSetting.COLS);

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
                        int k = i;
                        int w = j + 1;
                        // Find the key to swap with and perform the swap. Note the key can only be in front.
                        for(; k < KeyboardStateSetting.ROWS; k++)
                        {
                            for(; w < KeyboardStateSetting.COLS; w++)
                            {
                                if (kbStateResult.TransformedKbState[k][w] == expectedKbState[i][j])
                                {
                                    // Swap with this key, and if the swapping is correct, were good to go.
                                    AnalysisGraphSystem.GenerateSignalSwapKeys(i, j, k, w);
                                    break;
                                }
                            }

                            w = 0;
                        }
                    }

                    LogUtils.Assert(kbStateResult.TransformedKbState[i][j] == expectedKbState[i][j], "Swapping key back to position failed for some reason.");
                }
            }

            if (!CheckStats(expectedKbState, kbStateResult.TransformedKbState, expectedC2k, c2kResult.CharacterToKey,
                expectedC2f, c2fResult.CharacterToFinger, expectedTotalSfbs, tfs.TotalSfbs, expectedTotalSfs, tfs.TotalSfs,
                expectedSfbDistPerKey, tfs.SfbDistancePerKey, expectedSfsDistPerKey, tfs.SfsDistancePerKey,
                expectedTotalScissors, scissorsResult.TotalResult,
                expectedScissorsPerKey, scissorsResult.PerKeyResult,
                expectedKeyLag, keyLagResult.PerKeyResult))
            {
                return false;
            }

            sw.Stop();
            LogUtils.LogInfo($"Finished key swap tests. ({sw.ElapsedMilliseconds} ms)");

            return true;
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
                // Since we know swapping and reevaluation isn't completely messed up, go ahead and start the generation.
                // The score is the total key score.
                BestNSwapsOptimizer();
            }
        }

        private void BestNSwapsOptimizer()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            var keyLagResult = (KeyLagResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyLag];
            var kbStateResult = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];

            long totalSwaps = 0;
            double bestScore = 10000000;
            int swapPasses = 0;
            int optDepth = 2;

            (int, int, int, int)[] bestSwaps = new (int, int, int, int)[optDepth];
            double swapsPerSecond = 0;

            while(PerformNBestSwaps(0, optDepth, ref bestScore, ref totalSwaps, keyLagResult, bestSwaps))
            {
                swapsPerSecond = totalSwaps / (timer.ElapsedMilliseconds / 1000.0);
                LogUtils.LogInfo($"Performed pass: Swaps per second {swapsPerSecond}. Best score: {bestScore}");

                // Only do the one swap we computed this round.
                for(int i = 0; i < optDepth; i++)
                {
                    AnalysisGraphSystem.GenerateSignalSwapKeys(bestSwaps[i].Item1, bestSwaps[i].Item2, bestSwaps[i].Item3, bestSwaps[i].Item4);
                }

                swapPasses += 1;
            }

            swapsPerSecond = totalSwaps / (timer.ElapsedMilliseconds / 1000.0);

            timer.Stop();
            LogUtils.LogInfo($"Generated layout in {timer.ElapsedMilliseconds / 1000.0} sseconds with {totalSwaps} swaps. {swapPasses} total passes");
            LogUtils.LogInfo($"Swaps per second: {swapsPerSecond}");
            kbStateResult.LogKbState();
            LogUtils.LogInfo($"Score: {bestScore}");
        }

        private bool PerformNBestSwaps(int currentDepth, int totalDepth, ref double bestScore, ref long totalSwaps, KeyLagResult keyLag,
            Span<(int, int, int, int)> bestSwaps)
        {
            if(currentDepth >= totalDepth)
            {
                if(keyLag.TotalResult < bestScore && bestScore - keyLag.TotalResult > 1e-6)
                {
                    bestScore = keyLag.TotalResult;
                    return true;
                }
                return false;
            }

            int rows = KeyboardStateSetting.ROWS;
            int cols = KeyboardStateSetting.COLS;
            bool setBest = false;

            for(int i = 0; i < rows; i++)
            {
                for(int j = 0; j < cols; j++)
                {
                    int k = i;
                    int w = j + 1;

                    for(; k < rows; k++)
                    {
                        for(; w < cols; w++)
                        {
                            AnalysisGraphSystem.GenerateSignalSwapKeys(i, j, k, w);

                            if(PerformNBestSwaps(currentDepth + 1, totalDepth, ref bestScore, ref totalSwaps, keyLag, bestSwaps))
                            {
                                bestSwaps[currentDepth] = (i, j, k, w);
                                setBest = true;
                            }

                            AnalysisGraphSystem.GenerateSignalSwapKeys(i, j, k, w);
                            totalSwaps += 2;
                        }

                        w = 0;
                    }
                }
            }

            return setBest;
        }

        private void BestSwapOptimizer()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            var keyLagResult = (KeyLagResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyLag];
            var kbStateResult = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];

            long totalSwaps = 0;
            double bestScore = 10000000;

            while (PerformBestSwap(keyLagResult, ref bestScore, ref totalSwaps));

            timer.Stop();

            LogUtils.LogInfo($"Generated layout in {timer.ElapsedMilliseconds / 1000.0} sseconds with {totalSwaps} swaps");
            kbStateResult.LogKbState();
            LogUtils.LogInfo($"Score: {bestScore}");

        }

        // Finds the best swap in the layout, takes it, then does it again until no more swaps are available.
        private bool PerformBestSwap(KeyLagResult keyLag, ref double bestScore, ref long totalSwaps)
        {
            int rows = KeyboardStateSetting.ROWS;
            int cols = KeyboardStateSetting.COLS;

            int bestSwap1i = 0, bestSwap1j = 0, bestSwap2i = 0, bestSwap2j = 0;

            for(int i = 0; i < rows; i++)
            {
                for(int j = 0; j < cols; j++)
                {
                    int k = i;
                    int w = j + 1;

                    for(; k < rows; k++)
                    {
                        for(; w < cols; w++)
                        {
                            AnalysisGraphSystem.GenerateSignalSwapKeys(i, j, k, w);

                            if(keyLag.TotalResult < bestScore)
                            {
                                bestScore = keyLag.TotalResult;
                                bestSwap1i = i;
                                bestSwap1j = j;
                                bestSwap2i = k;
                                bestSwap2j = w;
                            }

                            AnalysisGraphSystem.GenerateSignalSwapKeys(i, j, k, w);
                            totalSwaps += 2;
                        }

                        w = 0;
                    }
                }
            }

            if (bestSwap1i != bestSwap2i || bestSwap1j != bestSwap2j)
            {
                AnalysisGraphSystem.GenerateSignalSwapKeys(bestSwap1i, bestSwap1j, bestSwap2i, bestSwap2j);
                totalSwaps++;
                return true;
            }

            return false;
        }
    }
}
