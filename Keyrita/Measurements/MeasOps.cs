using Keyrita.Gui;

namespace Keyrita.Measurements
{
    /// <summary>
    /// Enumerates all the user-facing measurements which can be done.
    /// </summary>
    public enum eMeasurements
    {
        [UIData("SFB")]
        SameFingerBigram,
        [UIData("SFS")]
        SameFingerSkipgrams,
        [UIData("Inrolls")]
        InRolls,
        [UIData("Outrolls")]
        OutRolls,
        [UIData("Alternations")]
        Alternations,
        [UIData("Left Hand Usage")]
        LeftHandBalance,
        [UIData("Right Hand Usage")]
        RightHandBalance,
        [UIData("Redirects")]
        Redirects,
        [UIData("Bad Redirects")]
        BadRedirects,
        [UIData("Finger Usage")]
        FingerUsage,
    }
}
