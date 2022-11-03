using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keyrita.Settings;

namespace Keyrita.Generate
{
    /// <summary>
    /// A total of 12 bytes for the table entry, but unfortunately it will likely be aligned up to 16 bytes because it includes a long.
    /// If we want a mega entry, we will use 12 mb
    /// But we probably want closer to 200 mega entries
    /// </summary>
    struct TableEntry
    {
        public TableEntry()
        {
            OptimalLayoutIndex = -1;
            TotalSfbs = -1;
        }

        public int OptimalLayoutIndex { get; set; }

        /// <summary>
        /// Sanity checks to make sure we are highly likely to have the right value in the case of a hash collision.
        /// It's very unlikely that we get the exact same values on any two nodes, let alone collision nodes.
        /// </summary>
        public long TotalSfbs { get; set; }
    }

    struct OptimalLayout
    {
        public OptimalLayout()
        {
            Layout = new byte[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];
        }

        public byte[,] Layout { get; set; }
    }

    /// <summary>
    /// A table to hold informaiton about previously proecssed layouts.
    /// </summary>
    public class TranspositionTable
    {
        const int TABLE_SIZE = 0xFFFFFF;

        public TranspositionTable(int maxOptimalLayouts)
        {
            mTable = new TableEntry[TABLE_SIZE];
            mOptimalLayouts = new OptimalLayout[maxOptimalLayouts];
        }

        int GetEntryIndex(int hash)
        {
            return hash & TABLE_SIZE;
        }

        public bool EntryMatches(int entry, int totalSfbs)
        {
            return mTable[entry].TotalSfbs == totalSfbs;
        }

        public int GetEntry(int entry, int totalSfbs)
        {
            return mTable[entry].OptimalLayoutIndex;
        }

        public void SetEntry(int entry, int optimalLayoutIndex, int totalSfbs)
        {
            mTable[entry].OptimalLayoutIndex = optimalLayoutIndex;
            mTable[entry].TotalSfbs = totalSfbs;
        }

        public int AddOptimalLayout(byte[][] optimalLayout)
        {
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    mOptimalLayouts[mOptimalLayoutIndex].Layout[i, j] = optimalLayout[i][j];
                }
            }

            return mOptimalLayoutIndex++;
        }

        private OptimalLayout[] mOptimalLayouts;
        private int mOptimalLayoutIndex = 0;

        private TableEntry[] mTable;
    }
}
