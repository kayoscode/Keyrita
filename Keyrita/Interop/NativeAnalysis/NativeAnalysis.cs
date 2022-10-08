using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Keyrita.Interop.NativeAnalysis
{
    public class NativeAnalysis
    {
        [DllImport("NativeAnalysisDll.dll", CharSet = CharSet.Unicode, SetLastError = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern long AnalyzeDataset([In] [MarshalAs(UnmanagedType.LPWStr)] string dataset, int datasetSize,
                                                 [In] [MarshalAs(UnmanagedType.LPWStr)] string validCharset, int charsetSize,
                                                 IntPtr charFreq,
                                                 IntPtr bigramFreq,
                                                 IntPtr trigramFreq,
                                                 IntPtr skipGramFreq,
                                                 IntPtr progress);

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
            double[] progress)
        {
            var h_progress = GCHandle.Alloc(progress, GCHandleType.Pinned);

            charFreq = new uint[validCharset.Length];
            bigramFreq = new uint[validCharset.Length, validCharset.Length];
            trigramFreq = new uint[validCharset.Length, validCharset.Length, validCharset.Length];
            skipGramFreq = new uint[SKIPGRAM_DEPTH, validCharset.Length, validCharset.Length];

            var h_charFreq = GCHandle.Alloc(charFreq, GCHandleType.Pinned);
            var h_bigramFreq = GCHandle.Alloc(bigramFreq, GCHandleType.Pinned);
            var h_trigramFreq = GCHandle.Alloc(trigramFreq, GCHandleType.Pinned);
            var h_skipgramFreq = GCHandle.Alloc(skipGramFreq, GCHandleType.Pinned);

            long charCount = AnalyzeDataset(dataset, dataset.Count(), validCharset, validCharset.Count(),
                h_charFreq.AddrOfPinnedObject(), h_bigramFreq.AddrOfPinnedObject(),
                h_trigramFreq.AddrOfPinnedObject(), h_skipgramFreq.AddrOfPinnedObject(),
                h_progress.AddrOfPinnedObject());

            h_charFreq.Free();
            h_bigramFreq.Free();
            h_trigramFreq.Free();
            h_skipgramFreq.Free();
            h_progress.Free();

            return charCount;
        }
    }
}
