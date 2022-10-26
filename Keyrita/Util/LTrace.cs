using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Keyrita.Util
{
    internal class AssertFailException : Exception
    {
        public AssertFailException(string msg) :
            base(msg)
        {
        }
    }

    internal static class LogUtils
    {
        static LogUtils()
        {
            Trace.Listeners.Add(new TextWriterTraceListener("Output.log", "Debug"));
            LogInfo("Starting trace services");
        }

        public static void LogInfo(string info)
        {
            Trace.TraceInformation(info);
            Trace.Flush();
        }

        public static void LogError(string err)
        {
            Trace.TraceError(err);
            Trace.Flush();
        }

        public static void LogWarning(string warning)
        {
            Trace.TraceWarning(warning);
            Trace.Flush();
        }

        /// <summary>
        /// Triggers the debugger and throws an exception if the condition fails.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="msg"></param>
        /// <exception cref="AssertFailException"></exception>
        public static void Assert(bool condition, string msg)
        {
            if(!condition)
            {
#if (DEBUG)
                if(Debugger.IsAttached)
                {
                    Debugger.Break();
                }
#endif

                if(msg != "")
                {
                    LogError(msg);
                }

                LogError(Environment.StackTrace);
            }
        }

        /// <summary>
        /// Throws if the assert fails.
        /// </summary>
        /// <param name="condition"></param>
        public static void Assert(bool condition)
        {
            Assert(condition, "");
        }
    }
}
