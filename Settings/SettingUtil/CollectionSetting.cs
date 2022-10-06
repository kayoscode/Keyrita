using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

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
    public abstract class ElementSetSetting : SettingBase, ICollectionSetting<object>
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

        public IEnumerable<object> Collection => mCollection;
        protected ISet<object> mCollection = new HashSet<object>();
        public IEnumerable<object> DefaultCollection => mDefaultCollection;
        protected ISet<object> mDefaultCollection = new HashSet<object>();

        // Items which will be added to the set should go here.
        protected ISet<object> mPendingAdditions = new HashSet<object>();
        protected ISet<object> mPendingRemovals = new HashSet<object>();

        /// <summary>
        /// Object which should be filled when setting to new limits.
        /// </summary>
        protected ISet<object> mNewLimits = new HashSet<object>();

        #region Public manipulation interface

        public void AddElement(object element)
        {
            mPendingAdditions.Add(element);
            TrySetToPending();
        }

        public void RemoveElement(object element)
        {
            mPendingRemovals.Add(element);
            TrySetToPending();
        }

        #endregion

        public override void Load()
        {
        }

        public override void Save()
        {
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
        protected virtual void ChangeLimits(ISet<object> newLimits)
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
        public void SetupPendingState(ISet<object> newValue)
        {
            // Remove every element that's not in the new collection, and add the ones that aren't there.
            foreach (var nextItem in mNewLimits)
            {
                if (!mCollection.Contains(nextItem))
                {
                    mPendingAdditions.Add(nextItem);
                }
            }

            foreach (var nextItem in mCollection)
            {
                if (!mNewLimits.Contains(nextItem))
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

    /// <summary>
    /// Forces each element in the collection setting to be the same type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ElementSetSetting<T> : ElementSetSetting, ICollectionSetting<T>
    {
        public ElementSetSetting(string settingName, eSettingAttributes attributes)
            : base(settingName, attributes)
        {
        }

        IEnumerable<T> ICollectionSetting<T>.Collection => Collection.Cast<T>();
        IEnumerable<T> ICollectionSetting<T>.DefaultCollection => DefaultCollection.Cast<T>();

        public void AddElement(T element)
        {
            base.AddElement(element);
        }

        public void RemoveElement(T element)
        {
            base.RemoveElement(element);
        }
    }
}
