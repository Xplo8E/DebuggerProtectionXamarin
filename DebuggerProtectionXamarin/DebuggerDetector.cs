using System;
using System.Runtime.InteropServices;
using ObjCRuntime;

namespace DebuggerProtectionXamarin
{
    /// <summary>
    /// Provides methods for preventing and detecting debugger attachment in iOS applications.
    /// </summary>
    public static class DebuggerDetector
    {
        [DllImport("/usr/lib/system/libsystem_c.dylib")]
        private static extern IntPtr dlerror();

        [DllImport("/usr/lib/system/libdyld.dylib")]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport("/usr/lib/system/libdyld.dylib")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("/usr/lib/system/libsystem_kernel.dylib")]
        private static extern int getppid();

        private const int RTLD_GLOBAL = 0x8;
        private const int RTLD_NOW = 0x2;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int PtraceDelegate(int request, int pid, IntPtr addr, IntPtr data);

        private const int PT_DENY_ATTACH = 31;

        /// <summary>
        /// Logs a message with a timestamp.
        /// </summary>
        private static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DebuggerDetector: {message}");
        }

        /// <summary>
        /// Attempts to prevent debugger attachment.
        /// </summary>
        public static void PreventDebugging()
        {
            Log("Attempting to prevent debugging");

            if (Runtime.Arch != Arch.DEVICE)
            {
                Log("Not running on a physical device. Skipping prevention attempt.");
                return;
            }

            try
            {
                Log("Attempting to open dynamic linker");
                IntPtr handle = dlopen(null, RTLD_GLOBAL | RTLD_NOW);
                if (handle == IntPtr.Zero)
                {
                    IntPtr errorPtr = dlerror();
                    string error = errorPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(errorPtr) : "Unknown error";
                    Log($"Failed to open dynamic linker. Error: {error}");
                    return;
                }
                Log("Successfully opened dynamic linker");

                Log("Attempting to find ptrace symbol");
                IntPtr symbolPtr = dlsym(handle, "ptrace");
                if (symbolPtr == IntPtr.Zero)
                {
                    IntPtr errorPtr = dlerror();
                    string error = errorPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(errorPtr) : "Unknown error";
                    Log($"Failed to find ptrace symbol. Error: {error}");
                    return;
                }
                Log("Successfully found ptrace symbol");

                Log("Creating delegate for ptrace function");
                var ptraceFunction = Marshal.GetDelegateForFunctionPointer<PtraceDelegate>(symbolPtr);
                
                Log("Calling ptrace function");
                int result = ptraceFunction(PT_DENY_ATTACH, 0, IntPtr.Zero, IntPtr.Zero);

                Log(result == 0
                    ? "Successfully prevented debugger attachment"
                    : $"Failed to prevent debugger attachment. Result: {result}");
            }
            catch (Exception ex)
            {
                Log($"Error while trying to prevent debugging: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Detects if the application is being debugged by checking the parent process ID.
        /// </summary>
        /// <returns>True if the application is being debugged, false otherwise.</returns>
        public static bool IsBeingDebugged()
        {
            var ppid = getppid();
            // ppid = 2;
            bool isDebugged = ppid != 1;
            Log($"Parent PID: {ppid}, IsBeingDebugged: {isDebugged}");
            Console.WriteLine($"DebuggerDetector: Parent PID: {ppid}, IsBeingDebugged: {isDebugged}");

            return isDebugged;
        }
    }
}
