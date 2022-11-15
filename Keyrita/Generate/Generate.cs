using System;
using System.Collections.Generic;
using System.Diagnostics;
using Keyrita.Measurements;
using Keyrita.Analysis;
using Keyrita.Analysis.AnalysisUtil;
using Keyrita.Settings;
using Keyrita.Util;
using System.Threading;
using System.Threading.Tasks;

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

        protected void SetupAnalysis(AnalysisGraph graph)
        {
            // Make sure required nodes are installed. 
            graph.InstallNode(eMeasurements.LayoutScore);

            // Just do a quick analysis cycle to get our initial conditions.
            graph.ResolveGraph();
            graph.PreprocessSwapKeysResolveOrder();
            // All future results will be obtained with the swap key functionality for max efficiency.
        }

        protected bool CheckStats(byte[][] expectedKbState, byte[][] kbStateResult, 
                                (int, int)[] expectedC2k, (int, int)[] c2k,
                                int[] expectedC2f, int[] c2f,
                                double expectedSfbDistance, double sfbTotalDistance,
                                double expectedSfsDistance, double sfsTotalDistance,
                                double expectedWeightdScissors, double totalWeightedScissors,
                                double expectedKeyLag, double keyLag,
                                long expectedInRolls, long inRolls,
                                long expectedOutRools, long outRolls,
                                long expectedRedirects, long redirects,
                                long expectedBadRedirects, long badRedirects,
                                long expectedOneHands, long oneHands,
                                long expectedAlternations, long alternations,
                                double expectedKbScore, double kbScore)

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
            if(!Utils.AreClose(expectedSfbDistance, sfbTotalDistance) ||
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

            // Check trigram stats.
            if(expectedInRolls != inRolls ||
                expectedOutRools != outRolls ||
                expectedRedirects != redirects ||
                expectedBadRedirects != badRedirects ||
                expectedOneHands != oneHands ||
                expectedAlternations != alternations)
            {
                LogUtils.Assert(false, "Trigram stats should have returned back to the expected value after a double swap");
                return false;
            }

            // Check keyboard score.
            if(!Utils.AreClose(expectedKbScore, kbScore))
            {
                LogUtils.Assert(false, "Layout score should have returned back to the expected value after a double swap");
                return false;
            }

            return true;
        }

        protected bool TestSwaps(AnalysisGraph graph)
        {
            Random random = new Random();

            // Values to compare against.
            var kbStateResult = (TransformedKbStateResult)graph.ResolvedNodes[eInputNodes.TransfomedKbState];
            var tfs = (TwoFingerStatsResult)graph.ResolvedNodes[eInputNodes.TwoFingerStats];
            var c2kResult = (TransformedCharacterToKeyResult)graph.ResolvedNodes[eInputNodes.TransformedCharacterToKey];
            var c2fResult = (TransformedCharacterToFingerAsIntResult)graph.ResolvedNodes[eInputNodes.TransformedCharacterToFingerAsInt];
            var scissorsResult = (ScissorsResult)graph.ResolvedNodes[eInputNodes.ScissorsIntermediate];
            var keyLagResult = (KeyLagResult)graph.ResolvedNodes[eInputNodes.KeyLag];
            var trigramStatsResult = (TrigramStatsResult)graph.ResolvedNodes[eInputNodes.TrigramStats];
            var layoutScoreResult = (LayoutScoreResult)graph.ResolvedNodes[eMeasurements.LayoutScore];

            var expectedKbState = Utils.CopyDoubleArray(kbStateResult.TransformedKbState);
            var expectedC2k = Utils.CopyArray(c2kResult.CharacterToKey);
            var expectedC2f = Utils.CopyArray(c2fResult.CharacterToFinger);
            var expectedTotalSfbDistance = tfs.TotalSfbDistance;
            var expectedTotalSfsDistance = tfs.TotalSfsDistance;
            var expectedWeightedScissors = scissorsResult.TotalWeightedResult;
            var expectedKeyLag = keyLagResult.TotalResult;
            var expectedInRolls = trigramStatsResult.InRolls;
            var expectedOutRolls = trigramStatsResult.OutRolls;
            var expectedRedirects = trigramStatsResult.TotalRedirects;
            var expectedBadRedirects = trigramStatsResult.TotalBadRedirects;
            var expectedOneHands = trigramStatsResult.TotalOneHands;
            var expectedAlternations = trigramStatsResult.TotalAlternations;
            var expectedLayoutScore = layoutScoreResult.TotalScore;

            int swapCount = 1000;

            LogUtils.LogInfo("Starting key swap tests.");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            for(int j = 0; j <= 1; j++)
            {
                for (int i = 0; i < swapCount; i++)
                {
                    int k1i = random.Next(0, KeyboardStateSetting.ROWS);
                    int k1j = random.Next(0, KeyboardStateSetting.COLS);
                    int k2i = random.Next(0, KeyboardStateSetting.ROWS);
                    int k2j = random.Next(0, KeyboardStateSetting.COLS);

                    // It never makes sense to swap a key with itself, wont even bother to make sure that behaves properly.
                    if (k1i == k2i && k1j == k2j) continue;

                    graph.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);

                    if(j == 1)
                    {
                        graph.GenerateSignalSwapBack();
                    }
                    else
                    {
                        graph.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);
                    }

                    if (!CheckStats(expectedKbState, kbStateResult.TransformedKbState, expectedC2k, c2kResult.CharacterToKey,
                        expectedC2f, c2fResult.CharacterToFinger,
                        expectedTotalSfbDistance, tfs.TotalSfbDistance, expectedTotalSfsDistance, tfs.TotalSfsDistance,
                        expectedWeightedScissors, scissorsResult.TotalWeightedResult,
                        expectedKeyLag, keyLagResult.TotalResult,
                        expectedInRolls, trigramStatsResult.InRolls,
                        expectedOutRolls, trigramStatsResult.OutRolls,
                        expectedRedirects, trigramStatsResult.TotalRedirects,
                        expectedBadRedirects, trigramStatsResult.TotalBadRedirects,
                        expectedOneHands, trigramStatsResult.TotalOneHands,
                        expectedAlternations, trigramStatsResult.TotalAlternations,
                        expectedLayoutScore, layoutScoreResult.TotalScore))
                    {
                        return false;
                    }
                }
            }

            // Now perform completely random swaps, but this time don't swap back!
            // At the end we will swap the keyboard back to its initial position and compare to the expected results.
            for (int i = 0; i < swapCount; i++)
            {
                int k1i = random.Next(0, KeyboardStateSetting.ROWS);
                int k1j = random.Next(0, KeyboardStateSetting.COLS);

                int k2i = random.Next(0, KeyboardStateSetting.ROWS);
                int k2j = random.Next(0, KeyboardStateSetting.COLS);

                // It never makes sense to swap a key with itself, wont even bother to make sure that behaves properly.
                if (k1i == k2i && k1j == k2j) continue;

                graph.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);
            }

            SwapKbToSetState(kbStateResult, expectedKbState, graph);

            if (!CheckStats(expectedKbState, kbStateResult.TransformedKbState, expectedC2k, c2kResult.CharacterToKey,
                expectedC2f, c2fResult.CharacterToFinger,
                expectedTotalSfbDistance, tfs.TotalSfbDistance, expectedTotalSfsDistance, tfs.TotalSfsDistance,
                expectedWeightedScissors, scissorsResult.TotalWeightedResult,
                expectedKeyLag, keyLagResult.TotalResult,
                expectedInRolls, trigramStatsResult.InRolls,
                expectedOutRolls, trigramStatsResult.OutRolls,
                expectedRedirects, trigramStatsResult.TotalRedirects,
                expectedBadRedirects, trigramStatsResult.TotalBadRedirects,
                expectedOneHands, trigramStatsResult.TotalOneHands,
                expectedAlternations, trigramStatsResult.TotalAlternations,
                expectedLayoutScore, layoutScoreResult.TotalScore))
            {
                return false;
            }

            sw.Stop();
            LogUtils.LogInfo($"Finished key swap tests. ({sw.ElapsedMilliseconds} ms)");

            return true;
        }

        private void SwapKbToSetState(TransformedKbStateResult kbStateResult, byte[][] nextState, AnalysisGraph graph)
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
                                    graph.GenerateSignalSwapKeys(i, j, k, w);
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
            AnalysisGraph graph = AnalysisGraphSystem.MainAnalysisGraph;

            SetupAnalysis(graph);

            long totalSwaps = 0;

            var kbStateResult = (TransformedKbStateResult)graph.ResolvedNodes[eInputNodes.TransfomedKbState];
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
            if (TestSwaps(graph))
            {
                timer.Start();
                var layoutScore = (LayoutScoreResult)graph.ResolvedNodes[eMeasurements.LayoutScore];

                // Set initial best layout to the current one.
                bestScore = layoutScore.TotalScore;
                bestLayout = Utils.CopyKeyboardState(kbStateResult.TransformedKbState, bestLayout, rows, cols);

                // Optimize to depth n.
                BestNSwapsOptimizer(depth, graph, layoutScore, lockedKeys);

                if (layoutScore.TotalScore < bestScore)
                {
                    bestScore = layoutScore.TotalScore;
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

        private class GenerateThreadInfo
        {
            public GenerateThreadInfo(AnalysisGraph graph, int batchSize, int numBatches)
            {
                this.graph = graph;
                BatchSize = batchSize;
                NumBatches = numBatches;
            }
    
            public AnalysisGraph graph { get; }
            public int BatchSize { get; }
            public int NumBatches { get; }
        }

        public void GenerateBetterLayoutThreadInstance(object generateThreadInfo)
        {
            GenerateThreadInfo info = generateThreadInfo as GenerateThreadInfo;
            LogUtils.Assert(info != null);

            AnalysisGraph graph = info.graph;

            SetupAnalysis(graph);

            Random random = new Random();
            long totalSwaps = 0;

            var kbStateResult = (TransformedKbStateResult)graph.ResolvedNodes[eInputNodes.TransfomedKbState];
            int rows = KeyboardStateSetting.ROWS;
            int cols = KeyboardStateSetting.COLS;
            int optimiaztionBatchSize = info.BatchSize;
            int batches = info.NumBatches;
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
            if (TestSwaps(graph))
            {
                timer.Start();
                var layoutScoreResult = (LayoutScoreResult)graph.ResolvedNodes[eMeasurements.LayoutScore];

                // Set initial best layout to the current one.
                bestScore = layoutScoreResult.TotalScore;
                bestLayout = Utils.CopyKeyboardState(kbStateResult.TransformedKbState, bestLayout, rows, cols);

                for(int batch = 0; batch < batches; batch++)
                {
                    int k1i = 0;
                    int k1j = 0;
                    int k2i = 0;
                    int k2j = 0;

                    for (int count = 0; count < optimiaztionBatchSize; count++)
                    {
                        int numSwaps = random.Next((int)(15 * (1 - (count / numOptimizations))) + 1) + 5;

                        for (int i = 0; i < numSwaps; i++)
                        {
                            k1i = random.Next(rows);
                            k1j = random.Next(cols);
                            k2i = random.Next(rows);
                            k2j = random.Next(cols);

                            if (k1i != k2i && k1j != k2j &&
                                !lockedKeys[k1i, k1j] && !lockedKeys[k2i, k2j])
                            {
                                graph.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);
                            }
                            else
                            {
                                i -= 1;
                            }
                        }

                        // Optimize the layout. 
                        long swaps = BestSwapOptimizer(layoutScoreResult, graph, lockedKeys);
                        totalSwaps += swaps;

                        if (layoutScoreResult.TotalScore < bestScore)
                        {
                            bestScore = layoutScoreResult.TotalScore;
                            bestLayout = Utils.CopyKeyboardState(kbStateResult.TransformedKbState, bestLayout, rows, cols);
                        }
                    }

                    for (int i = 0; i < 50; i++)
                    {
                        k1i = random.Next(rows);
                        k1j = random.Next(cols);
                        k2i = random.Next(rows);
                        k2j = random.Next(cols);

                        if (k1i != k2i && k1j != k2j &&
                            !lockedKeys[k1i, k1j] && !lockedKeys[k2i, k2j])
                        {
                            graph.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);
                        }
                        else
                        {
                            i -= 1;
                        }
                    }
                }

                SwapKbToSetState(kbStateResult, bestLayout, graph);

                timer.Stop();
                double olps = numOptimizations / (timer.ElapsedMilliseconds / 1000);
                LogUtils.LogInfo($"Optimized layouts per second: {olps}");
                LogUtils.LogInfo($"Best score: {bestScore}");
                LogUtils.LogInfo($"Average swaps per optimization: {totalSwaps / numOptimizations}");
                LogUtils.LogInfo($"Total swaps: {totalSwaps}");
            }
        }

        /// <summary>
        /// Given the current layout, locked keys, and much more, find the best possible arrangement.
        /// When finished, set the screen to use the new layout.
        /// This class needs to hijack the analysis system. (nothing else can do analysis in the meantime). 
        /// </summary>
        public void GenerateBetterLayout()
        {
            const int batchSize = 500;
            const int numBatches = 5;

            int numCores = Utils.GetNumCores();
            Thread[] generateThreads = new Thread[numCores];
            AnalysisGraph[] graphsOnThread = new AnalysisGraph[numCores];

            for(int i = 0; i < numCores; i++)
            {
                graphsOnThread[i] = AnalysisGraphSystem.CreateNewGraph();
                generateThreads[i] = new Thread(new ParameterizedThreadStart(GenerateBetterLayoutThreadInstance));
                generateThreads[i].Priority = ThreadPriority.Highest;

                generateThreads[i].Start(new GenerateThreadInfo(graphsOnThread[i], batchSize, numBatches));
            }

            Task finalTask = new Task(() =>
            {
                for(int i = 0; i < generateThreads.Length; i++)
                {
                    generateThreads[i].Join();
                }

                int bestScoreIndex = 0;
                double bestScore = ((LayoutScoreResult)graphsOnThread[bestScoreIndex].ResolvedNodes[eMeasurements.LayoutScore]).TotalScore;

                // After all threads finish, find the best of the generated layouts, and set it to the active layout.
                for(int i = 0; i < generateThreads.Length; i++)
                {
                    double score = ((LayoutScoreResult)graphsOnThread[i].ResolvedNodes[eMeasurements.LayoutScore]).TotalScore;

                    if(score < bestScore)
                    {
                        bestScore = score;
                        bestScoreIndex = i;
                    }
                }

                string chars = SettingState.MeasurementSettings.CharFrequencyData.AvailableCharSet;
                char[,] newKbState = new char[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];
                var bestKbState = ((TransformedKbStateResult)graphsOnThread[bestScoreIndex].ResolvedNodes[eInputNodes.TransfomedKbState]).TransformedKbState;

                for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
                {
                    for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                    {
                        newKbState[i, j] = chars[bestKbState[i][j]];
                    }
                }

                SettingState.KeyboardSettings.KeyboardState.SetKeyboardState(newKbState);
            });

            finalTask.Start(TaskScheduler.FromCurrentSynchronizationContext());
        }

        private long BestSwapOptimizer(LayoutScoreResult layoutScoreResult, AnalysisGraph graph, bool[,] lockedKeys)
        {
            long totalSwaps = 0;
            double bestScore = 10000000;

            while (PerformBestSwap(layoutScoreResult, graph, ref bestScore, ref totalSwaps, lockedKeys));

            return totalSwaps;
        }

        // Finds the best swap in the layout, takes it, then does it again until no more swaps are available.
        private bool PerformBestSwap(LayoutScoreResult layoutScore, AnalysisGraph graph, ref double bestScore, ref long totalSwaps, bool[,] lockedKeys)
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

                            graph.GenerateSignalSwapKeys(i, j, k, w);

                            if(layoutScore.TotalScore < bestScore && bestScore - layoutScore.TotalScore > 1e-6)
                            {
                                bestScore = layoutScore.TotalScore;
                                bestSwap1i = i;
                                bestSwap1j = j;
                                bestSwap2i = k;
                                bestSwap2j = w;
                                setBest = true;
                            }

                            graph.GenerateSignalSwapBack();
                            totalSwaps += 2;
                        }

                        w = 0;
                    }
                }
            }

            if (setBest)
            {
                graph.GenerateSignalSwapKeys(bestSwap1i, bestSwap1j, bestSwap2i, bestSwap2j);
            }

            return setBest;
        }

        private long BestNSwapsOptimizer(int depth, AnalysisGraph graph, LayoutScoreResult layoutScore, bool[,] lockedKeys)
        {
            long totalSwaps = 0;
            double bestScore = 10000000;
            int optDepth = depth;

            (int, int, int, int)[] bestSwaps = new (int, int, int, int)[optDepth];

            while(PerformNBestSwaps(graph, 0, optDepth, ref bestScore, ref totalSwaps, lockedKeys, layoutScore, bestSwaps))
            {
                // Only do the one swap we computed this round.
                for(int i = 0; i < optDepth; i++)
                {
                    graph.GenerateSignalSwapKeys(bestSwaps[i].Item1, bestSwaps[i].Item2, bestSwaps[i].Item3, bestSwaps[i].Item4);
                }
            }

            return totalSwaps;
        }

        private bool PerformNBestSwaps(AnalysisGraph graph ,int currentDepth, int totalDepth, ref double bestScore, ref long totalSwaps, bool[,] lockedKeys, LayoutScoreResult layoutScore,
            Span<(int, int, int, int)> bestSwaps)
        {
            if(currentDepth >= totalDepth)
            {
                if(layoutScore.TotalScore < bestScore && bestScore - layoutScore.TotalScore > 1e-6)
                {
                    bestScore = layoutScore.TotalScore;
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

                            graph.GenerateSignalSwapKeys(i, j, k, w);

                            if(PerformNBestSwaps(graph, currentDepth + 1, totalDepth, ref bestScore, ref totalSwaps, lockedKeys, layoutScore, bestSwaps))
                            {
                                bestSwaps[currentDepth] = (i, j, k, w);
                                setBest = true;
                            }

                            graph.GenerateSignalSwapKeys(i, j, k, w);
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
