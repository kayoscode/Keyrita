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
            
            // For each key, swap it with every other key, then swap back, and make sure we have the same results.
            for(int k1i = 0; k1i < KeyboardStateSetting.ROWS; k1i++)
            {
                for(int k1j = 0; k1j < KeyboardStateSetting.COLS; k1j++)
                {
                    for(int k2i = 0; k2i < KeyboardStateSetting.ROWS; k2i++)
                    {
                        for(int k2j = 0; k2j < KeyboardStateSetting.COLS; k2j++)
                        {
                            // Doesn't make sense to swap a key with itself.
                            if (k1i == k2i && k1j == k2j) continue;

                            AnalysisGraphSystem.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);
                            AnalysisGraphSystem.GenerateSignalSwapKeys(k1i, k1j, k2i, k2j);

                            // Check the transformed kb state.
                            if(!Utils.CompareDoubleArray(expectedKbState, kbStateResult.TransformedKbState))
                            {
                                LogUtils.Assert(false, "Transformed kb state should have returned back to the expected value after a double swap");
                                return false;
                            }

                            // Check character to key results.
                            if(!Utils.CompareArray(expectedC2k, c2kResult.CharacterToKey))
                            {
                                LogUtils.Assert(false, "Character to key state should have returned back to the expected value after a double swap");
                                return false;
                            }

                            // Check character to finger results.
                            if(!Utils.CompareArray(expectedC2f, c2fResult.CharacterToFinger))
                            {
                                LogUtils.Assert(false, "Character to finger state should have returned back to the expected value after a double swap");
                                return false;
                            }

                            // Two finger stats.
                            if(expectedTotalSfbs != tfs.TotalSfbs || expectedTotalSfs != tfs.TotalSfs ||
                                !Utils.CompareRectArrayDoubles(expectedSfbDistPerKey, tfs.SfbDistancePerKey) ||
                                !Utils.CompareRectArrayDoubles(expectedSfsDistPerKey, tfs.SfsDistancePerKey))
                            {
                                LogUtils.Assert(false, "Same finger stats should have been preserved");
                                return false;
                            }
                        }
                    }
                }
            }

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

            long swapCount = MeasureLayoutsPerSecond();
            LogUtils.LogInfo($"Swaps per second: {swapCount}");
        }
    }
}
