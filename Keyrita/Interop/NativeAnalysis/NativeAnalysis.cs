using System.Runtime.InteropServices;

namespace Keyrita.Interop.NativeAnalysis
{
    public class NativeAnalysis
    {
        [DllImport("NativeAnalysisDll.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int TestFromDLL(int a);
    }
}
