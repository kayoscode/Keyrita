using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Keyrita.Settings;

namespace Keyrita.Interop.NativeAnalysis
{
    public class NativeAnalysis
    {
        [DllImport("user32.dll")]
        public static extern short VkKeyScan(char c);

        [DllImport("user32.dll", SetLastError=true)]
        public static extern int ToAscii(
            uint uVirtKey,
            uint uScanCode,
            byte[] lpKeyState,
            out uint lpChar,
            uint flags);

        [DllImport("NativeAnalysisDll.dll", CharSet = CharSet.Unicode, SetLastError = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern long AnalyzeDataset([In] [MarshalAs(UnmanagedType.LPWStr)] string dataset, int datasetSize,
                                                 [In] [MarshalAs(UnmanagedType.LPWStr)] string validCharset, int charsetSize,
                                                 IntPtr charFreq,
                                                 IntPtr bigramFreq,
                                                 IntPtr trigramFreq,
                                                 IntPtr skipGramFreq,
                                                 IntPtr progress,
                                                 IntPtr isCanceled);

        public const int SKIPGRAM_DEPTH = 3;

        /// <summary>
        /// Public interface to analyze data.
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="validCharset"></param>
        /// <param name="charFreq"></param>
        /// <param name="bigramFreq"></param>
        /// <param name="trigramFreq"></param>
        /// <param name="skipGramFreq"></param>
        /// <returns></returns>
        public static long AnalyzeDataset(string dataset, string validCharset, out uint[] charFreq,
            out uint[,] bigramFreq, out uint[,,] trigramFreq, out uint[,,] skipGramFreq,
            double[] progress, bool[] isCanceled)
        {
            var h_progress = GCHandle.Alloc(progress, GCHandleType.Pinned);
            var h_canceled = GCHandle.Alloc(isCanceled, GCHandleType.Pinned);

            charFreq = new uint[validCharset.Length];
            bigramFreq = new uint[validCharset.Length, validCharset.Length];
            trigramFreq = new uint[validCharset.Length, validCharset.Length, validCharset.Length];
            skipGramFreq = new uint[SKIPGRAM_DEPTH, validCharset.Length, validCharset.Length];

            var h_charFreq = GCHandle.Alloc(charFreq, GCHandleType.Pinned);
            var h_bigramFreq = GCHandle.Alloc(bigramFreq, GCHandleType.Pinned);
            var h_trigramFreq = GCHandle.Alloc(trigramFreq, GCHandleType.Pinned);
            var h_skipgramFreq = GCHandle.Alloc(skipGramFreq, GCHandleType.Pinned);

            long charCount = 0;
            try
            {
                charCount = AnalyzeDataset(dataset, dataset.Count(), validCharset, validCharset.Count(),
                    h_charFreq.AddrOfPinnedObject(), h_bigramFreq.AddrOfPinnedObject(),
                    h_trigramFreq.AddrOfPinnedObject(), h_skipgramFreq.AddrOfPinnedObject(),
                    h_progress.AddrOfPinnedObject(), h_canceled.AddrOfPinnedObject());
            }
            finally
            {
                h_charFreq.Free();
                h_bigramFreq.Free();
                h_trigramFreq.Free();
                h_skipgramFreq.Free();
                h_progress.Free();
                h_canceled.Free();
            }

            return charCount;
        }

        [DllImport("NativeAnalysisDll.dll", CharSet = CharSet.Unicode, SetLastError = true,
        CallingConvention = CallingConvention.Cdecl)]
        private static extern long MeasureTotalSFBs(IntPtr keyboardState, IntPtr bigramFreq, IntPtr keyToFinger, int numValidChars);

        /// <summary>
        /// Returns the total number of bigrams in the layout.
        /// </summary>
        /// <param name="keyboardState"></param>
        /// <param name="bigramFreq"></param>
        /// <param name=""></param>
        public static long MeasureTotalSFBs(byte[,] keyboardState, uint[,] bigramFreq, int[,] keyToFinger)
        {
            var h_keyboardState = GCHandle.Alloc(keyboardState, GCHandleType.Pinned);
            var h_bigramFreq = GCHandle.Alloc(bigramFreq, GCHandleType.Pinned);
            var h_keyToFinger = GCHandle.Alloc(keyToFinger, GCHandleType.Pinned);

            long totalSfbs = 0;
            try
            {
                totalSfbs = MeasureTotalSFBs(h_keyboardState.AddrOfPinnedObject(), h_bigramFreq.AddrOfPinnedObject(), h_keyToFinger.AddrOfPinnedObject(), bigramFreq.GetLength(0));
            }
            finally
            {
                h_keyToFinger.Free();
                h_bigramFreq.Free();
                h_keyboardState.Free();
            }

            return totalSfbs;
        }

        /// <summary>
        /// Finds the 10 most common sfbs on the layout.
        /// </summary>
        public static void MeasureMostCommonSFBs(){}
    }
}
