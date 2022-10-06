using Keyrita.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;

namespace Keyrita.Settings.SettingUtil
{
    /// <summary>
    /// Readonly interface to a collection
    /// </summary>
    public interface ICollectionSetting<T> : ISetting
    {
        IEnumerable<T> Collection { get; }
        IEnumerable<T> DefaultCollection { get; }
        void AddElement(T element);
        void RemoveElement(T element);
    }

    /// <summary>
    /// A setting holding a set of data.
    /// </summary>
    public abstract class ElementSetSetting<T> : SettingBase, ICollectionSetting<T>
    {
        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="attributes"></param>
        public ElementSetSetting(string settingName, eSettingAttributes attributes) :
            base(settingName, attributes)
        {
        }

        public override bool HasValue => Collection != null;
        protected override bool ValueHasChanged => mPendingAdditions.Count() > 0 || mPendingRemovals.Count() > 0;

        public IEnumerable<T> Collection => mCollection;
        protected ISet<T> mCollection = new HashSet<T>();
        public IEnumerable<T> DefaultCollection => mDefaultCollection;
        protected ISet<T> mDefaultCollection = new HashSet<T>();

        // Items which will be added to the set should go here.
        protected ISet<T> mPendingAdditions = new HashSet<T>();
        protected ISet<T> mPendingRemovals = new HashSet<T>();

        /// <summary>
        /// Object which should be filled when setting to new limits.
        /// </summary>
        protected ISet<T> mNewLimits = new HashSet<T>();

        #region Public manipulation interface

        public void AddElement(T element)
        {
            mPendingAdditions.Add(element);
            TrySetToPending();
        }

        public void RemoveElement(T element)
        {
            mPendingRemovals.Add(element);
            TrySetToPending();
        }

        #endregion

        protected override void Load(string text)
        {
            string[] set = text.Split(" ");
            var newElements = new HashSet<T>();

            foreach (string character in set)
            {
                if (TextSerializers.TryParse(character, out T loadedChar))
                {
                    newElements.Add(loadedChar);
                }
            }

            SetupPendingState(newElements);
            TrySetToPending();
        }

        protected override void Save(XmlWriter writer)
        {
            // Convert the enum value to a string and write it to the stream writer.
            string uniqueName = this.GetSettingUniqueId();

            writer.WriteStartElement(uniqueName);

            foreach(T element in Collection)
            {
                writer.WriteString(TextSerializers.ToText(element));
                writer.WriteString(" ");
            }

            writer.WriteEndElement();
        }

        protected override void Action()
        {
        }

        protected override sealed void ChangeLimits()
        {
            mNewLimits.Clear();
            ChangeLimits(mNewLimits);
        }

        /// <summary>
        /// Should be overriden instead of ChangeLimits().
        /// The newLimits variable should be filled with the correct new 
        /// collection of items.
        /// </summary>
        /// <param name="newLimits"></param>
        protected virtual void ChangeLimits(ISet<T> newLimits)
        {
        }

        /// <summary>
        /// Resets the collection to fit new limits.
        /// </summary>
        protected override void SetToNewLimits()
        {
            SetupPendingState(mNewLimits);
            mNewLimits.Clear();
            TrySetToPending();
        }

        protected override void SetToDefault()
        {
            SetupPendingState(mDefaultCollection);
            TrySetToPending();
        }

        /// <summary>
        /// Sets pending to remove items that aren't in the new set and to add items that are.
        /// </summary>
        /// <param name="newValue"></param>
        public void SetupPendingState(ISet<T> newValue)
        {
            // Remove every element that's not in the new collection, and add the ones that aren't there.
            foreach (var nextItem in newValue)
            {
                if (!mCollection.Contains(nextItem))
                {
                    mPendingAdditions.Add(nextItem);
                }
            }

            foreach (var nextItem in mCollection)
            {
                if (!newValue.Contains(nextItem))
                {
                    mPendingRemovals.Add(nextItem);
                }
            }
        }

        /// <summary>
        /// Update the current list by adding and removing items properly.
        /// </summary>
        protected override void TrySetToPending()
        {
            if (mPendingRemovals.Count > 0 || mPendingAdditions.Count > 0)
            {
                string description = "";
                if (mPendingAdditions.Count > 0)
                {
                    description += $"Adding {string.Join(" ", mPendingAdditions)} to collection. ";
                }

                if (mPendingRemovals.Count > 0)
                {
                    description += $"Removing {string.Join(" ", mPendingRemovals)} from collection. ";
                }

                SettingTransaction(description, () =>
                {
                    // Remove each item in the removals, then add each one in the additions.
                    foreach (var removal in mPendingRemovals)
                    {
                        mCollection.Remove(removal);
                    }

                    foreach (var item in mPendingAdditions)
                    {
                        mCollection.Add(item);
                    }

                    mPendingAdditions.Clear();
                    mPendingRemovals.Clear();
                });
            }
        }
    }
}
