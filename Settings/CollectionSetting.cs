using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Keyrita.Settings
{
    /// <summary>
    /// Readonly interface to a collection
    /// </summary>
    public interface ICollectionSetting<T> : ISetting
    {
        IEnumerable<T> Collection { get; }
        void AddElement(T element);
        void RemoveElement(T element);
    }

    public class CollectionSetting : SettingBase, ICollectionSetting<object>
    {
        public CollectionSetting(string settingName, eSettingAttributes attributes) : 
            base(settingName, attributes)
        {
        }

        public override bool HasValue => Collection != null;

        protected override bool ValueHasChanged => mPendingAdditions.Count() > 0 || mPendingRemovals.Count() > 0;

        public IEnumerable<object> Collection => mCollection;
        protected List<object> mCollection = new List<object>();

        // Items which will be added to the set should go here.
        protected List<object> mPendingAdditions = new List<object>();
        protected List<object> mPendingRemovals = new List<object>();

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

        protected override void ChangeLimits()
        {
        }

        protected override void SetToDefault()
        {
            mPendingAdditions.Clear();
            mPendingRemovals.Clear();

            // Default is an empty list.
            mPendingRemovals.AddRange(mCollection);
            TrySetToPending();
        }

        protected override void SetToNewLimits()
        {
            // Nothing to do, this setting has no singular value.
        }

        /// <summary>
        /// Update the current list by adding and removing items properly.
        /// </summary>
        protected override void TrySetToPending()
        {
            if (mPendingRemovals.Count > 0 || mPendingAdditions.Count > 0)
            {
                string description = "";
                if(mPendingAdditions.Count > 0)
                {
                    description += $"Adding {string.Join(",", mPendingAdditions)} to collection. ";
                }

                if (mPendingRemovals.Count > 0)
                {
                    description += $"Removing {string.Join(",", mPendingRemovals)} from collection. ";
                }

                SettingTransaction(description, () =>
                {
                    // Start by adding each item, then remove.
                    mCollection.AddRange(mPendingAdditions);

                    foreach (var removal in mPendingRemovals)
                    {
                        mCollection.Remove(removal);
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
    public class CollectionSetting<T> : CollectionSetting, ICollectionSetting<T>
    {
        public CollectionSetting(string settingName, eSettingAttributes attributes) 
            : base(settingName, attributes)
        {
        }

        IEnumerable<T> ICollectionSetting<T>.Collection => base.Collection.Cast<T>();

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
