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

        public uint[,] BigramFreq => mBigramFreq;
        protected uint[,] mBigramFreq;
        public long BigramHitCount { get; protected set; }

        public uint[,,] TrigramFreq => mTrigramFreq;
        protected uint[,,] mTrigramFreq;
        public long TrigramHitCount { get; protected set; }

        protected uint[,,] SkipgramFreq => mSkipgramFreq;
        protected uint[,,] mSkipgramFreq;
        public long[] SkipgramHitCount { get; protected set; } = new long[NativeAnalysis.SKIPGRAM_DEPTH];

        public string UsedCharset => mUsedCharset;
        protected string mUsedCharset;

        public override bool HasValue => !(CharFreq == null || BigramFreq == null || TrigramFreq == null || SkipgramFreq == null || mUsedCharset == null);
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
                LogUtils.Assert(charIdx >= 0 && charIdx < UsedCharset.Length, "Sanity check failed.");
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
                LogUtils.Assert(idx1 >= 0 && idx1 < UsedCharset.Length, "Sanity check failed.");
                LogUtils.Assert(idx2 >= 0 && idx2 < UsedCharset.Length, "Sanity check failed.");

                if(BigramHitCount != 0)
                {
                    return mBigramFreq[idx1, idx2] / (double)BigramHitCount;
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

        protected override void SetDependencies()
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
                    long charCount = NativeAnalysis.AnalyzeDataset(fileText, mUsedCharset,
                        out mCharFreq, out mBigramFreq, out mTrigramFreq, out mSkipgramFreq,
                        mProgress, mIsCanceled);
                    mIsCanceled[0] = false;

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

        protected override void ChangeLimits()
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

                for (int i = 0; i < mBigramFreq.GetLength(0); i++)
                {
                    for (int j = 0; j < mBigramFreq.GetLength(1); j++)
                    {
                        BigramHitCount += mBigramFreq[i, j];
                    }
                }

                for (int i = 0; i < mTrigramFreq.GetLength(0); i++)
                {
                    for (int j = 0; j < mTrigramFreq.GetLength(1); j++)
                    {
                        for (int k = 0; k < mTrigramFreq.GetLength(2); k++)
                        {
                            TrigramHitCount += mTrigramFreq[i, j, k];
                        }
                    }
                }

                for (int i = 0; i < mSkipgramFreq.GetLength(0); i++)
                {
                    for (int j = 0; j < mSkipgramFreq.GetLength(1); j++)
                    {
                        for (int k = 0; k < mSkipgramFreq.GetLength(2); k++)
                        {
                            SkipgramHitCount[i] += mSkipgramFreq[i, j, k];
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
                    mBigramFreq = new uint[sqrtDatalen, sqrtDatalen];

                    for(int i = 0; i < sqrtDatalen; i++)
                    {
                        for(int j = 0; j < sqrtDatalen; j++)
                        {
                            TextSerializers.TryParse(bigramFreqData[i * sqrtDatalen + j], out mBigramFreq[i, j]);
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
                    mTrigramFreq = new uint[cbRootDatalen, cbRootDatalen, cbRootDatalen];

                    for(int i = 0; i < cbRootDatalen; i++)
                    {
                        for(int j = 0; j < cbRootDatalen; j++)
                        {
                            for(int k = 0; k < cbRootDatalen; k++)
                            {
                                TextSerializers.TryParse(trigramFreqData[(i * (cbRootDatalen * cbRootDatalen)) + (j * cbRootDatalen) + k], out mTrigramFreq[i, j, k]);
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

            mSkipgramFreq = new uint[allSkipgramNodes.Count(), mUsedCharset.Length, mUsedCharset.Length];
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
                                TextSerializers.TryParse(skipgramFreqData[i * sqrtDatalen + j], out mSkipgramFreq[ski, i, j]);
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
                        writer.WriteString(TextSerializers.ToText(BigramFreq[i, j]));
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
                            writer.WriteString(TextSerializers.ToText(TrigramFreq[i, j, k]));
                            writer.WriteString(" ");
                        }
                    }
                }
                writer.WriteEndElement();

                for(int i = 0; i < SkipgramFreq.GetLength(0); i++)
                {
                    writer.WriteStartElement($"SkipgramFreq{i}");
                    for(int j = 0; j < available.Length; j++)
                    {
                        for(int k = 0; k < available.Length; k++)
                        {
                            writer.WriteString(TextSerializers.ToText(SkipgramFreq[i, j, k]));
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
                SettingTransaction("Analyzing dataset", false, () =>
                {
                    // Now that we have reported the setting change, just make it so that we report the setting hasn't changed yet.
                    mValueHasChanged = false;
                });
            }
        }
    }
}
