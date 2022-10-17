using Keyrita.Gui;

namespace Keyrita.Measurements
{
    /// <summary>
    /// Enumerates all the user-facing measurements which can be done.
    /// </summary>
    public enum eMeasurements
    {
        [UIData("SFB", "Shows the bigrams which use the same finger")]
        SameFingerBigram,
        [UIData("SFS", "Shows the skipgrams which use the same finger")]
        SameFingerSkipgrams,
        [UIData("Rolls", "Shows in/out rolls")]
        Rolls,
        [UIData("Alternations", "Shows the alternation rate")]
        Alternations,
        [UIData("Hand Usage", "Shows the hand balance")]
        HandBalance,
        [UIData("Redirects", "Shows the redirection rate")]
        Redirects,
        [UIData("Finger Usage", "Shows finger balance stats")]
        FingerUsage,
    }
}
