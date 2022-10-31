using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Keyrita.Serialization;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Settings
{
    /// <summary>
    /// Holds the current scissor map state.
    /// </summary>
    public class ScissorMapSetting : SettingBase
    {
        public ScissorMapSetting() :
            base("Scissor Map", eSettingAttributes.Recall)
        {
            for(int i = 0; i < mScissorMapState.GetLength(0); i++)
            {
                for(int j = 0; j < mScissorMapState.GetLength(1); j++)
                {
                    mScissorMapState[i, j] = new List<(int, int)>();
                    mPendingScissorMapState[i, j] = new List<(int, int)>();
                    mDesiredScissorMapState[i, j] = new List<(int, int)>();
                }
            }
        }

        protected List<(int, int)>[,] mScissorMapState = new List<(int, int)>[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];
        protected List<(int, int)>[,] mPendingScissorMapState = new List<(int, int)>[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];
        protected List<(int, int)>[,] mDesiredScissorMapState = new List<(int, int)>[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];

        public override bool HasValue => mScissorMapState != null;
        protected override bool ValueHasChanged => MapMatches(mPendingScissorMapState, mScissorMapState) > 0;

        protected override void ChangeLimits()
        {
        }

        protected override void SetToNewLimits()
        {
        }

        public override void SetToDefault()
        {
            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    mDesiredScissorMapState[i, j].Clear();
                }
            }

            // The indices listed for each key indicate that the scissors for that key should accumulate all of them. Key 0, 0 gets scissors from 1, 1 and 2, 1 
            AddScissorIndices((0, 0), (1, 1));
            AddScissorIndices((0, 0), (2, 1));

            AddScissorIndices((0, 1), (2, 0));
            AddScissorIndices((0, 1), (2, 2));

            AddScissorIndices((0, 2), (2, 1));
            AddScissorIndices((0, 2), (2, 4));

            AddScissorIndices((0, 3), (2, 2));

            AddScissorIndices((0, 4), (2, 2));

            AddScissorIndices((0, 5), (2, 7));

            AddScissorIndices((0, 6), (2, 7));

            AddScissorIndices((0, 7), (2, 8));

            AddScissorIndices((0, 8), (2, 7));
            AddScissorIndices((0, 8), (2, 9));

            AddScissorIndices((0, 9), (1, 8));
            AddScissorIndices((0, 9), (2, 8));

            AddScissorIndices((1, 0), (2, 1));
            AddScissorIndices((1, 9), (2, 8));

            SetToDesiredValue();
        }

        protected void AddScissorIndices((int, int) idx1, (int, int) idx2)
        {
            mDesiredScissorMapState[idx1.Item1, idx1.Item2].Add(idx2);
            mDesiredScissorMapState[idx2.Item1, idx2.Item2].Add(idx1);
        }

        protected override void Load(string text)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(text);

            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    mDesiredScissorMapState[i, j].Clear();
                    string nodeName = $"key{i}_{j}";
                    XmlNode node = xml.SelectSingleNode("Keys/" + nodeName);

                    if(node != null && node.InnerText.Length > 0)
                    {
                        string[] indices = node.InnerText.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        for(int k = 0; k < indices.Length; k++)
                        {
                            if (TextSerializers.TryParse(indices[k], out (int, int) index))
                            {
                                mDesiredScissorMapState[i, j].Add(index);
                            }
                        }
                    }
                }
            }

            SetToDesiredValue();
        }

        protected override void Save(XmlWriter writer)
        {
            if(HasValue)
            {
                writer.WriteStartElement("Keys");
                for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
                {
                    for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                    {
                        var scissors = mScissorMapState[i, j];
                        string nodeName = $"key{i}_{j}";
                        writer.WriteStartElement(nodeName);

                        for(int k = 0; k < scissors.Count(); k++)
                        {
                            writer.WriteString(TextSerializers.ToText(scissors[k]));
                            writer.WriteString(" ");
                        }
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }
        }

        protected override void Action()
        {
        }

        /// <summary>
        /// Returns the character at a specified index.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public List<(int, int)> GetScissorsAt(int row, int col)
        {
            return mScissorMapState[row, col];
        }

        /// <summary>
        /// Sets the keyboard layout to desired.
        /// </summary>
        public override sealed void SetToDesiredValue()
        {
            CopyMap(mPendingScissorMapState, mDesiredScissorMapState);
            TrySetToPending();
        }

        protected override void TrySetToPending(bool userInitiated = false)
        {
            // If the pending keyboard state does not match the current keyboard state, start a setting transaction.
            var count = MapMatches(mPendingScissorMapState, mScissorMapState);

            if (count != 0)
            {
                var description = $"Changing {count} map items";

                SettingTransaction(description, userInitiated, () =>
                {
                    CopyMap(mScissorMapState, mPendingScissorMapState);
                });
            }
        }

        /// <summary>
        /// Copies keyboard2 to keyboard1.
        /// </summary>
        /// <param name="kb1"></param>
        /// <param name="kb2"></param>
        protected static void CopyMap(List<(int, int)>[,] s1, List<(int, int)>[,] s2)
        {
            for(int i = 0; i < s1.GetLength(0); i++)
            {
                for(int j = 0; j < s1.GetLength(1); j++)
                {
                    var copyVal = s2[i, j];
                    s1[i, j].Clear();
                    for(int k = 0; k < copyVal.Count(); k++)
                    {
                        s1[i, j].Add(copyVal[k]);
                    }
                }
            }
        }

        /// <summary>
        /// Returns whether two boards have the same state.
        /// Specifically returns the number of keys that don't match.
        /// </summary>
        /// <param name="kb1"></param>
        /// <param name="kb2"></param>
        /// <returns></returns>
        public static int MapMatches(List<(int, int)>[,] s1, List<(int, int)>[,] s2)
        {
            var count = 0;

            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    var v1 = s1[i, j];
                    var v2 = s2[i, j];

                    if(v1.Count != v2.Count)
                    {
                        count++;
                        continue;
                    }

                    for(int k = 0; k < v1.Count(); k++)
                    {
                        if(v1[k] != v2[k])
                        {
                            count++;
                            continue;
                        }
                    }
                }
            }

            return count;
        }
    }
}
