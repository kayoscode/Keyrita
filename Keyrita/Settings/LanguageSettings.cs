using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Keyrita.Interop.NativeAnalysis;
using Keyrita.Serialization;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Settings
{
    /// <summary>
    /// Stores information about the language based on the dataset.
    /// </summary>
    public class CharFrequencySetting : ProgressSetting
    {
        public CharFrequencySetting() 
            : base("Language Data", eSettingAttributes.RecallNoUndo)
        {
        }

        public uint[] CharFreq => mCharFreq;
        protected uint[] mCharFreq;
        public long CharHitCount { get; protected set; }

        public uint[][] BigramFreq => mBigramFreq;
        protected uint[][] mBigramFreq;
        public long BigramHitCount { get; protected set; }

        public uint[][][] TrigramFreq => mTrigramFreq;
        protected uint[][][] mTrigramFreq;
        public long TrigramHitCount { get; protected set; }

        // The commonality of trigrams that start with the first index and end with the second.
        public uint[][] Skipgram2Freq => mSkipgram2Freq;
        protected uint[][] mSkipgram2Freq;
        public long Skipgram2HitCount => TrigramHitCount;

        public uint[][][] SkipgramFreq => mSkipgramFreq;
        protected uint[][][] mSkipgramFreq;
        public long[] SkipgramHitCount { get; protected set; } = new long[NativeAnalysis.SKIPGRAM_DEPTH];

        public string AvailableCharSet => mUsedCharset;
        protected string mUsedCharset;

        public override bool HasValue => !(CharFreq == null || BigramFreq == null || TrigramFreq == null 
            || SkipgramFreq == null || mUsedCharset == null);
        protected override bool ValueHasChanged => mValueHasChanged;

        /// <summary>
        /// Gets the frequency of a character from the dataset.
        /// </summary>
        /// <param name="charIdx"></param>
        /// <returns></returns>
        public double GetCharFreq(int charIdx)
        {
            if(HasValue)
            {
                LogUtils.Assert(charIdx >= 0 && charIdx < AvailableCharSet.Length, "Sanity check failed.");
                return CharFreq[charIdx] / (double)CharHitCount;
            }

            return 0.0;
        }

        /// <summary>
        /// Returns the probability of c1 -> c2 in a row.
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public double GetBigramFreq(int idx1, int idx2)
        {
            if(HasValue)
            {
                LogUtils.Assert(idx1 >= 0 && idx1 < AvailableCharSet.Length, "Sanity check failed.");
                LogUtils.Assert(idx2 >= 0 && idx2 < AvailableCharSet.Length, "Sanity check failed.");

                if(BigramHitCount != 0)
                {
                    return mBigramFreq[idx1][idx2] / (double)BigramHitCount;
                }
            }

            return 0.0;
        }


        #region Progress Data

        public override double Progress => mProgress[0];
        protected double[] mProgress = new double[1];
        public override bool IsRunning => mIsRunning;
        protected bool mIsRunning = false;

        protected bool[] mIsCanceled = new bool[1] { false };
        public override void Cancel()
        {
            NotifyCanceled.NotifyGui(this);
            mIsRunning = false;
            mIsCanceled[0] = true;

            cancellationTokenSource.Cancel();

            cancellationTokenSource = new CancellationTokenSource();
            LoadTask = null;

            // Make sure we are on the main thread. Otherwise we should assert false, but continue with execution anyway.
            TrySetToPending();
        }

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Task LoadTask = null;

        #endregion

        private bool mValueHasChanged = false;

        protected override void Init()
        {
        }

        /// <summary>
        /// Returns a sorted list of valid characters.
        /// </summary>
        /// <returns></returns>
        public string GetCharList()
        {
            StringBuilder available = new StringBuilder();

            var charList = SettingState.KeyboardSettings.AvailableCharSet.Collection.ToList<char>();
            charList.Sort();
            foreach (char c in charList)
            {
                available.Append(c);
            }

            return available.ToString();
        }

        public void ClearData()
        {
            // Setting the charset to null will invalidate the dataset. Must be reloaded to be used again.
            mUsedCharset = null;
            mValueHasChanged = true;
            TrySetToPending();
        }

        /// <summary>
        /// Load the dataset using the native code!
        /// </summary>
        /// <param name="fileName"></param>
        public void LoadDataset(string fileText)
        {
            LogUtils.Assert(LoadTask == null, "We shouldn't be able to get here if were currently loading.");

            if (fileText == null) return;
            object mutex = new object();

            // Load the layout on thread.
            lock (mutex)
            {
                mIsRunning = true;
                mProgress[0] = 0;
                NotifyProgressBarStarted.NotifyGui(this);

                int currentThread = System.Threading.Thread.CurrentThread.ManagedThreadId;

                LoadTask = Task.Factory.StartNew(() =>
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    for (int i = 0; i < SkipgramHitCount.Length; i++)
                    {
                        SkipgramHitCount[i] = 0;
                    }

                    mUsedCharset = GetCharList();

                    // This method needs rectangle arrays, so create them, then convert them to appropriately.
                    long charCount = NativeAnalysis.AnalyzeDataset(fileText, mUsedCharset,
                        out mCharFreq, out uint[,] bgFreq, out uint[,,] tgFreq, out uint[,,] skgFreq,
                        mProgress, mIsCanceled);

                    mIsCanceled[0] = false;

                    // Copy the data to the member arrays.
                    mBigramFreq = new uint[bgFreq.GetLength(0)][];
                    for(int i = 0; i < bgFreq.GetLength(0); i++)
                    {
                        mBigramFreq[i] = new uint[bgFreq.GetLength(1)];
                        for(int j = 0; j < bgFreq.GetLength(1); j++)
                        {
                            mBigramFreq[i][j] = bgFreq[i, j];
                        }
                    }

                    mTrigramFreq = new uint[tgFreq.GetLength(0)][][];
                    for(int i = 0; i < tgFreq.GetLength(0); i++)
                    {
                        mTrigramFreq[i] = new uint[tgFreq.GetLength(1)][];
                        for(int j = 0; j < tgFreq.GetLength(1); j++)
                        {
                            mTrigramFreq[i][j] = new uint[tgFreq.GetLength(2)];
                            for(int k = 0; k < tgFreq.GetLength(2); k++)
                            {
                                mTrigramFreq[i][j][k] = tgFreq[i, j, k];
                            }
                        }
                    }

                    mSkipgramFreq = new uint[skgFreq.GetLength(0)][][];
                    for(int i = 0; i < skgFreq.GetLength(0); i++)
                    {
                        mSkipgramFreq[i] = new uint[skgFreq.GetLength(1)][];
                        for(int j = 0; j < skgFreq.GetLength(1); j++)
                        {
                            mSkipgramFreq[i][j] = new uint[skgFreq.GetLength(2)];
                            for(int k = 0; k < skgFreq.GetLength(2); k++)
                            {
                                mSkipgramFreq[i][j][k] = skgFreq[i, j, k];
                            }
                        }
                    }

                    if(charCount != -1)
                    {
                        SetHitCountData();
                    }

                    mValueHasChanged = true;
                    sw.Stop();
                    long time = sw.ElapsedMilliseconds;
                    LogUtils.LogInfo($"Analyzed dataset in {time} milliseconds");
                }, cancellationTokenSource.Token);

                LoadTask.ConfigureAwait(true).GetAwaiter().OnCompleted(() =>
                {
                    int newThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    LogUtils.Assert(currentThread == newThread, "This needs to be on the same thread");

                    mIsRunning = false;
                    cancellationTokenSource = new CancellationTokenSource();
                    LoadTask = null;

                    // Make sure we are on the main thread. Otherwise we should assert false, but continue with execution anyway.
                    TrySetToPending();
                });
            }
        }

        public override void SetToDefault()
        {
            // TODO: Load default dataset.
        }

        public override void SetToDesiredValue()
        {
            // The desired value is whatever the heck it is currently set to.
            TrySetToPending();
        }

        protected override void Action()
        {
            // Nothing to do.
        }

        protected override void ConformToLimits()
        {
            // This setting doesn't use a pending value!
        }

        private static readonly string WRAPPED_XML_ELEMENT_NAME = "SettingData";
        private static readonly string CHARSET_ELEMENT_NAME = "CharSet";
        private static readonly string CHARFREQ_ELEMENT_NAME = "CharFreq";
        private static readonly string BIGRAM_ELEMENT_NAME = "BigramFreq";
        private static readonly string TRIGRAM_ELEMENT_NAME = "TrigramFreq";

        /// <summary>
        /// After loading, we need to verify and set hit count data.
        /// </summary>
        private void SetHitCountData()
        {
            if (HasValue)
            {
                CharHitCount = 0;
                BigramHitCount = 0;
                TrigramHitCount = 0;

                for(int i = 0; i < SkipgramHitCount.Length; i++)
                {
                    SkipgramHitCount[i] = 0;
                }

                // Get the hit counts, hopefully this doesn't take too long to process.
                for (int i = 0; i < mCharFreq.Length; i++)
                {
                    CharHitCount += mCharFreq[i];
                }

                for (int i = 0; i < mBigramFreq.Length; i++)
                {
                    for (int j = 0; j < mBigramFreq[i].Length; j++)
                    {
                        BigramHitCount += mBigramFreq[i][j];
                    }
                }

                mSkipgram2Freq = new uint[mTrigramFreq.Length][];
                for (int i = 0; i < mTrigramFreq.Length; i++)
                {
                    mSkipgram2Freq[i] = new uint[mTrigramFreq[i].Length];

                    for (int j = 0; j < mTrigramFreq.Length; j++)
                    {
                        uint hitCount = 0;

                        for (int k = 0; k < mTrigramFreq.Length; k++)
                        {
                            hitCount += mTrigramFreq[i][k][j];
                        }

                        mSkipgram2Freq[i][j] = hitCount;
                        TrigramHitCount += hitCount;
                    }
                }

                for (int i = 0; i < mSkipgramFreq.Length; i++)
                {
                    for (int j = 0; j < mSkipgramFreq[i].Length; j++)
                    {
                        for (int k = 0; k < mSkipgramFreq[i][j].Length; k++)
                        {
                            SkipgramHitCount[i] += mSkipgramFreq[i][j][k];
                        }
                    }
                }

                var exBigramHitCount = CharHitCount - 1;
                LogUtils.Assert(exBigramHitCount == BigramHitCount, "Sanity check failed");
                var exTrigramHitCount = BigramHitCount - 1;
                LogUtils.Assert(exTrigramHitCount == TrigramHitCount, "Sanity check failed");

                var exSkipgramHitCount = new long[SkipgramHitCount.Length];
                exSkipgramHitCount[0] = TrigramHitCount - 1;
                LogUtils.Assert(exSkipgramHitCount[0] == SkipgramHitCount[0], "Sanity check failed");

                for (int i = 1; i < SkipgramHitCount.Length; i++)
                {
                    exSkipgramHitCount[i] = SkipgramHitCount[i - 1] - 1;
                    LogUtils.Assert(exSkipgramHitCount[i] == SkipgramHitCount[i], "Sanity check failed");
                }
            }
            else
            {
                LogUtils.Assert(false, "Shouldn't get here without a value.");
            }
        }

        protected override void Load(string text)
        {
            // We saved it as a sub-xml document. so just load it back up. ez pz
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(text);
            mValueHasChanged = true;

            // First load the used charset from the file.
            XmlNodeList charset = doc.GetElementsByTagName(CHARSET_ELEMENT_NAME);
            if(charset.Count == 1)
            {
                // Just load it in, theres a setting to validate it later.
                mUsedCharset = charset[0].InnerText;
            }

            // Load the frequency of each character.
            XmlNodeList charFrequency = doc.GetElementsByTagName(CHARFREQ_ELEMENT_NAME);
            if(charFrequency.Count == 1)
            {
                string[] charFreqData = charFrequency[0].InnerText.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);

                if(charFreqData.Length == mUsedCharset.Length)
                {
                    // We have valid frequency data.
                    mCharFreq = new uint[charFreqData.Length];

                    for (int i = 0; i < charFreqData.Length; i++)
                    {
                        TextSerializers.TryParse(charFreqData[i], out mCharFreq[i]);
                    }
                }
            }

            // Load the frequency of each bigram.
            XmlNodeList bigramFreq = doc.GetElementsByTagName(BIGRAM_ELEMENT_NAME);
            if(bigramFreq.Count == 1)
            {
                string[] bigramFreqData = bigramFreq[0].InnerText.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                double bigramFreqDataLen = (double)bigramFreqData.Length;
                int sqrtDatalen = (int)Math.Sqrt(bigramFreqDataLen);

                // The size needs to be a perfect square. k.
                if(bigramFreqDataLen == sqrtDatalen * sqrtDatalen && sqrtDatalen == mUsedCharset.Length)
                {
                    mBigramFreq = new uint[sqrtDatalen][];

                    for(int i = 0; i < sqrtDatalen; i++)
                    {
                        mBigramFreq[i] = new uint[sqrtDatalen];
                        for(int j = 0; j < sqrtDatalen; j++)
                        {
                            TextSerializers.TryParse(bigramFreqData[i * sqrtDatalen + j], out mBigramFreq[i][j]);
                        }
                    }
                }
            }

            // Load the frequency of each trigram.
            XmlNodeList trigramFreq = doc.GetElementsByTagName(TRIGRAM_ELEMENT_NAME);
            if(trigramFreq.Count == 1)
            {
                string[] trigramFreqData = trigramFreq[0].InnerText.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                double trigramFreqDataLen = (double)trigramFreqData.Length;
                int cbRootDatalen = (int)Math.Cbrt(trigramFreqDataLen);

                // The size needs to be a perfect cube. k.
                if(trigramFreqDataLen == cbRootDatalen * cbRootDatalen * cbRootDatalen && cbRootDatalen == mUsedCharset.Length)
                {
                    mTrigramFreq = new uint[cbRootDatalen][][];

                    for(int i = 0; i < cbRootDatalen; i++)
                    {
                        mTrigramFreq[i] = new uint[cbRootDatalen][];
                        for(int j = 0; j < cbRootDatalen; j++)
                        {
                            mTrigramFreq[i][j] = new uint[cbRootDatalen];
                            for(int k = 0; k < cbRootDatalen; k++)
                            {
                                TextSerializers.TryParse(trigramFreqData[(i * (cbRootDatalen * cbRootDatalen)) + (j * cbRootDatalen) + k], out mTrigramFreq[i][j][k]);
                            }
                        }
                    }
                }
            }

            // Finally load the frequency of each skipgram 0-n
            XmlNodeList allChildren = doc.ChildNodes[0].ChildNodes;
            List<XmlNode> allSkipgramNodes = new List<XmlNode>();

            foreach(XmlNode child in allChildren)
            {
                if (child.Name.Contains("SkipgramFreq"))
                {
                    allSkipgramNodes.Add(child);
                }
            }

            int skArrSize = mUsedCharset.Length;
            mSkipgramFreq = new uint[allSkipgramNodes.Count()][][];

            for (int i = 0; i < mSkipgramFreq.Length; i++)
            {
                mSkipgramFreq[i] = new uint[skArrSize][];
                for(int j = 0; j < mSkipgramFreq[i].Length; j++)
                {
                    mSkipgramFreq[i][j] = new uint[skArrSize];
                }
            }

            foreach(XmlNode child in allSkipgramNodes)
            {
                int ski = child.Name[child.Name.Length - 1] - '0';

                if(ski >= 0 && ski < allSkipgramNodes.Count)
                {
                    string[] skipgramFreqData = child.InnerText.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    double dataLen = (double)skipgramFreqData.Length;
                    int sqrtDatalen = (int)Math.Sqrt(dataLen);

                    if(dataLen == sqrtDatalen * sqrtDatalen && sqrtDatalen == mUsedCharset.Length)
                    {
                        // We've found a valid skg node.
                        for(int i = 0; i < sqrtDatalen; i++)
                        {
                            for(int j = 0; j < sqrtDatalen; j++)
                            {
                                TextSerializers.TryParse(skipgramFreqData[i * sqrtDatalen + j], out mSkipgramFreq[ski][i][j]);
                            }
                        }
                    }
                }
            }

            SetHitCountData();
        }

        protected override void Save(XmlWriter writer)
        {
            if(HasValue)
            {
                // We have to wrap the data in an element here to be valid xml.
                writer.WriteStartElement(WRAPPED_XML_ELEMENT_NAME);

                string available = mUsedCharset;
                writer.WriteStartElement(CHARSET_ELEMENT_NAME);
                foreach (char c in available)
                {
                    writer.WriteString(TextSerializers.ToText(c));
                }
                writer.WriteEndElement();

                // Character freq
                writer.WriteStartElement(CHARFREQ_ELEMENT_NAME);
                for(int i = 0; i < available.Length; i++)
                {
                    writer.WriteString(TextSerializers.ToText(CharFreq[i]));
                    writer.WriteString(" ");
                }
                writer.WriteEndElement();

                // Bigrams
                writer.WriteStartElement(BIGRAM_ELEMENT_NAME);
                for(int i = 0; i < available.Length; i++)
                {
                    for(int j = 0; j < available.Length; j++)
                    {
                        // Writing to the file in order {available[i]: avaialble[j]}
                        writer.WriteString(TextSerializers.ToText(BigramFreq[i][j]));
                        writer.WriteString(" ");
                    }
                }
                writer.WriteEndElement();

                writer.WriteStartElement(TRIGRAM_ELEMENT_NAME);
                for(int i = 0; i < available.Length; i++)
                {
                    for(int j = 0; j < available.Length; j++)
                    {
                        for(int k = 0; k < available.Length; k++)
                        {
                            writer.WriteString(TextSerializers.ToText(TrigramFreq[i][j][k]));
                            writer.WriteString(" ");
                        }
                    }
                }
                writer.WriteEndElement();

                for(int i = 0; i < SkipgramFreq.Length; i++)
                {
                    writer.WriteStartElement($"SkipgramFreq{i}");
                    for(int j = 0; j < available.Length; j++)
                    {
                        for(int k = 0; k < available.Length; k++)
                        {
                            writer.WriteString(TextSerializers.ToText(SkipgramFreq[i][j][k]));
                            writer.WriteString(" ");
                        }
                    }
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }

        protected override void SetToNewLimits()
        {
        }

        protected override void TrySetToPending(bool userInitiated = false)
        {
            // We aren't using a pending value, so lets just report a settings transaction occurred.
            if (ValueHasChanged)
            {
                InitiateSettingChange("Analyzing dataset", false, () =>
                {
                    // Now that we have reported the setting change, just make it so that we report the setting hasn't changed yet.
                    mValueHasChanged = false;
                });
            }
        }
    }
}
