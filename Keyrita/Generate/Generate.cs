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
                                double expectedSfbDistance, double sfbTotalDistance,
                                double expectedSfsDistance, double sfsTotalDistance,
                                double expectedWeightdScissors, double totalWeightedScissors,
                                double expectedKeyLag, double keyLag)

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
                !Utils.AreClose(expectedSfbDistance, sfbTotalDistance) ||
                !Utils.AreClose(expectedSfsDistance, sfsTotalDistance))
            {
                LogUtils.Assert(false, "Same finger stats should have been preserved");
                return false;
            }

            // Check scissors.
            if(!Utils.AreClose(expectedWeightdScissors, totalWeightedScissors))
            {
                LogUtils.Assert(false, "Scissors should have returned back to the expected value after a double swap");
                return false;
            }

            // Check key speed.
            if(!Utils.AreClose(expectedKeyLag, keyLag))
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
            var expectedTotalSfbDistance = tfs.TotalSfbDistance;
            var expectedTotalSfsDistance = tfs.TotalSfsDistance;
            var expectedTotalSfbs = tfs.TotalSfbs;
            var expectedTotalSfs = tfs.TotalSfs;
            var expectedWeightedScissors = scissorsResult.TotalWeightedResult;
            var expectedKeyLag = keyLagResult.TotalResult;

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
                    expectedTotalSfbDistance, tfs.TotalSfbDistance, expectedTotalSfsDistance, tfs.TotalSfsDistance,
                    expectedWeightedScissors, scissorsResult.TotalWeightedResult,
                    expectedKeyLag, keyLagResult.TotalResult))
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

            SwapKbToSetState(kbStateResult, expectedKbState);

            if (!CheckStats(expectedKbState, kbStateResult.TransformedKbState, expectedC2k, c2kResult.CharacterToKey,
                expectedC2f, c2fResult.CharacterToFinger, expectedTotalSfbs, tfs.TotalSfbs, expectedTotalSfs, tfs.TotalSfs,
                expectedTotalSfbDistance, tfs.TotalSfbDistance, expectedTotalSfsDistance, tfs.TotalSfsDistance,
                expectedWeightedScissors, scissorsResult.TotalWeightedResult,
                expectedKeyLag, keyLagResult.TotalResult))
            {
                return false;
            }

            sw.Stop();
            LogUtils.LogInfo($"Finished key swap tests. ({sw.ElapsedMilliseconds} ms)");

            return true;
        }

        private void SwapKbToSetState(TransformedKbStateResult kbStateResult, byte[][] nextState)
        {
            // Swap every key that's out of place with its correct key to get to the set state.
            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    if (nextState[i][j] != kbStateResult.TransformedKbState[i][j])
                    {
                        int k = i;
                        int w = j + 1;
                        // Find the key to swap with and perform the swap. Note the key can only be in front.
                        for(; k < KeyboardStateSetting.ROWS; k++)
                        {
                            for(; w < KeyboardStateSetting.COLS; w++)
                            {
                                if (kbStateResult.TransformedKbState[k][w] == nextState[i][j])
                                {
                                    // Swap with this key, and if the swapping is correct, were good to go.
                                    AnalysisGraphSystem.GenerateSignalSwapKeys(i, j, k, w);
                                    break;
                                }
                            }

                            w = 0;
                        }
                    }

                    LogUtils.Assert(kbStateResult.TransformedKbState[i][j] == nextState[i][j], "Swapping key back to position failed for some reason.");
                }
            }
        }

        public void OptimizeLayout(int depth)
        {
            SetupAnalysis();

            long totalSwaps = 0;

            var kbStateResult = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            int rows = KeyboardStateSetting.ROWS;
            int cols = KeyboardStateSetting.COLS;
            Stopwatch timer = new Stopwatch();
            var lockedKeys = SettingState.KeyboardSettings.LockedKeys.KeyStateCopy;

            double bestScore = 1000000;
            byte[][] bestLayout = new byte[rows][];
            for (int i = 0; i < rows; i++)
            {
                bestLayout[i] = new byte[cols];
            }

            // Only proceed if the cache swapping system isn't obviously broken.
            if (TestSwaps())
            {
                timer.Start();
                var keyLagResult = (KeyLagResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyLag];

                // Set initial best layout to the current one.
                bestScore = keyLagResult.TotalResult;
                bestLayout = Utils.CopyKeyboardState(kbStateResult.TransformedKbState, bestLayout, rows, cols);

                // Optimize to depth n.
                BestNSwapsOptimizer(depth, keyLagResult, lockedKeys);

                if (keyLagResult.TotalResult < bestScore)
                {
                    bestScore = keyLagResult.TotalResult;
                    bestLayout = Utils.CopyKeyboardState(kbStateResult.TransformedKbState, bestLayout, rows, cols);
                }

                timer.Stop();
                LogUtils.LogInfo($"Best score: {bestScore}");
                LogUtils.LogInfo($"Total swaps: {totalSwaps}");

                string chars = SettingState.MeasurementSettings.CharFrequencyData.AvailableCharSet;
                char[,] newKbState = new char[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];
                for(int i = 0; i < rows; i++)
                {
                    for(int j = 0; j < cols; j++)
                    {
                        newKbState[i, j] = chars[bestLayout[i][j]];
                    }
                }

                // Swap to the best keyboard layout and do a depth 2 search to make sure we get over local optima in case we converged late.
                SettingState.KeyboardSettings.KeyboardState.SetKeyboardState(newKbState);
            }
        }

        /// <summary>
        /// Given the current layout, locked keys, and much more, find the best possible arrangement.
        /// When finished, set the screen to use the new layout.
        /// This class needs to hijack the analysis system. (nothing else can do analysis in the meantime). 
        /// </summary>
        public void GenerateBetterLayout()
        {
            SetupAnalysis();
            long totalSwaps = 0;

            var kbStateResult = (TransformedKbStateResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.TransfomedKbState];
            int rows = KeyboardStateSetting.ROWS;
            int cols = KeyboardStateSetting.COLS;
            int optimiaztionBatchSize = 5000;
            int batches = 5;
            double numOptimizations = (double)optimiaztionBatchSize * batches;
            Stopwatch timer = new Stopwatch();
            var lockedKeys = SettingState.KeyboardSettings.LockedKeys.KeyStateCopy;

            double bestScore = 1000000;
            byte[][] bestLayout = new byte[rows][];
            for (int i = 0; i < rows; i++)
            {
                bestLayout[i] = new byte[cols];
            }

            // Only proceed if the cache swapping system isn't obviously broken.
            if (TestSwaps())
            {
                //FastRandom random = new FastRandom((uint)mRand.Next(100000));
                timer.Start();
                var keyLagResult = (KeyLagResult)AnalysisGraphSystem.ResolvedNodes[eInputNodes.KeyLag];

                // Set initial best layout to the current one.
                bestScore = keyLagResult.TotalResult;
                bestLayout = Utils.CopyKeyboardState(kbStateResult.TransformedKbState, bestLayout, rows, cols);

                for(int batch = 0; batch < batches; batch++)
                {
                    int k1i = 0;
                    int k1j = 0;
                    int k2i = 0;
                    int k2j = 0;

                    for (int count = 0; count < optimiaztionBatchSize; count++)
                    {
                        int numSwaps = mRand.Next((int)(20 * (1 - (count / numOptimizations))) + 1) + 5;

                        for (int i = 0; i < numSwaps; i++)
                        {
                            k1i = mRand.Next(rows);
                            k1j = mRand.Next(cols);
                            k2i = mRand.Next(rows);
                            k2j = mRand.Next(cols);

                            if (k1i != k2i && k1j != k2j &&
                                !lockedKeys[k1i, k1j] && !lockedKeys[k2i, k2j])
                            {
                                AnalysisGraphSystem.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);
                            }
                            else
                            {
                                i -= 1;
                            }
                        }

                        // Optimize the layout. 
                        long swaps = BestSwapOptimizer(keyLagResult, lockedKeys);
                        totalSwaps += swaps;

                        if (keyLagResult.TotalResult < bestScore)
                        {
                            bestScore = keyLagResult.TotalResult;
                            bestLayout = Utils.CopyKeyboardState(kbStateResult.TransformedKbState, bestLayout, rows, cols);
                        }
                    }

                    LogUtils.LogInfo($"Batch complete: {bestScore}");
                    for (int i = 0; i < 2500; i++)
                    {
                        k1i = mRand.Next(rows);
                        k1j = mRand.Next(cols);
                        k2i = mRand.Next(rows);
                        k2j = mRand.Next(cols);

                        if (k1i != k2i && k1j != k2j &&
                            !lockedKeys[k1i, k1j] && !lockedKeys[k2i, k2j])
                        {
                            AnalysisGraphSystem.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);
                        }
                        else
                        {
                            i -= 1;
                        }
                    }
                }

                SwapKbToSetState(kbStateResult, bestLayout);
                BestNSwapsOptimizer(2, keyLagResult, lockedKeys);

                if (keyLagResult.TotalResult < bestScore)
                {
                    bestScore = keyLagResult.TotalResult;
                    bestLayout = Utils.CopyKeyboardState(kbStateResult.TransformedKbState, bestLayout, rows, cols);
                }

                timer.Stop();
                double olps = numOptimizations / (timer.ElapsedMilliseconds / 1000);
                LogUtils.LogInfo($"Optimized layouts per second: {olps}");
                LogUtils.LogInfo($"Best score: {bestScore}");
                LogUtils.LogInfo($"Average swaps per optimization: {totalSwaps / numOptimizations}");
                LogUtils.LogInfo($"Total swaps: {totalSwaps}");

                string chars = SettingState.MeasurementSettings.CharFrequencyData.AvailableCharSet;
                char[,] newKbState = new char[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];
                for(int i = 0; i < rows; i++)
                {
                    for(int j = 0; j < cols; j++)
                    {
                        newKbState[i, j] = chars[bestLayout[i][j]];
                    }
                }

                // Swap to the best keyboard layout and do a depth 2 search to make sure we get over local optima in case we converged late.

                SettingState.KeyboardSettings.KeyboardState.SetKeyboardState(newKbState);
            }
        }

        private long BestSwapOptimizer(KeyLagResult keyLagResult, bool[,] lockedKeys)
        {
            long totalSwaps = 0;
            double bestScore = 10000000;

            while (PerformBestSwap(keyLagResult, ref bestScore, ref totalSwaps, lockedKeys));

            return totalSwaps;
        }

        // Finds the best swap in the layout, takes it, then does it again until no more swaps are available.
        private bool PerformBestSwap(KeyLagResult keyLag, ref double bestScore, ref long totalSwaps, bool[,] lockedKeys)
        {
            int rows = KeyboardStateSetting.ROWS;
            int cols = KeyboardStateSetting.COLS;
            bool setBest = false;
            int bestSwap1i = 0;
            int bestSwap1j = 0;
            int bestSwap2i = 0;
            int bestSwap2j = 0;

            for(int i = 0; i < rows; i++)
            {
                for(int j = 0; j < cols; j++)
                {
                    int k = i;
                    int w = j + 1;

                    if (lockedKeys[i, j]) continue;

                    for(; k < rows; k++)
                    {
                        for(; w < cols; w++)
                        {
                            if (lockedKeys[k, w]) continue;

                            AnalysisGraphSystem.GenerateSignalSwapKeys(i, j, k, w);

                            if(keyLag.TotalResult < bestScore && bestScore - keyLag.TotalResult > 1e-6)
                            {
                                bestScore = keyLag.TotalResult;
                                bestSwap1i = i;
                                bestSwap1j = j;
                                bestSwap2i = k;
                                bestSwap2j = w;
                                setBest = true;
                            }

                            AnalysisGraphSystem.GenerateSignalSwapKeys(i, j, k, w);
                            totalSwaps += 2;
                        }

                        w = 0;
                    }
                }
            }

            if (setBest)
            {
                AnalysisGraphSystem.GenerateSignalSwapKeys(bestSwap1i, bestSwap1j, bestSwap2i, bestSwap2j);
            }

            return setBest;
        }

        private long BestNSwapsOptimizer(int depth, KeyLagResult keyLagResult, bool[,] lockedKeys)
        {
            long totalSwaps = 0;
            double bestScore = 10000000;
            int optDepth = depth;

            (int, int, int, int)[] bestSwaps = new (int, int, int, int)[optDepth];

            while(PerformNBestSwaps(0, optDepth, ref bestScore, ref totalSwaps, lockedKeys, keyLagResult, bestSwaps))
            {
                // Only do the one swap we computed this round.
                for(int i = 0; i < optDepth; i++)
                {
                    AnalysisGraphSystem.GenerateSignalSwapKeys(bestSwaps[i].Item1, bestSwaps[i].Item2, bestSwaps[i].Item3, bestSwaps[i].Item4);
                }
            }

            return totalSwaps;
        }

        private bool PerformNBestSwaps(int currentDepth, int totalDepth, ref double bestScore, ref long totalSwaps, bool[,] lockedKeys, KeyLagResult keyLag,
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

                    if (lockedKeys[i, j]) continue;

                    for(; k < rows; k++)
                    {
                        for(; w < cols; w++)
                        {
                            if (lockedKeys[k, w]) continue;

                            AnalysisGraphSystem.GenerateSignalSwapKeys(i, j, k, w);

                            if(PerformNBestSwaps(currentDepth + 1, totalDepth, ref bestScore, ref totalSwaps, lockedKeys, keyLag, bestSwaps))
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
    }
}
