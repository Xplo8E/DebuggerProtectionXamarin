using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ObjCRuntime;

namespace DebuggerProtectionXamarin;

public static class DebuggerDetector
{
    private static void Log(string message)
    {
        // In production, you might want to use a proper logging framework
        // or send logs to a file or remote server
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }

    static DebuggerDetector()
    {
        Log("DebuggerDetector: Static constructor called");
    }

    [DllImport("/usr/lib/system/libsystem_kernel.dylib")]
    private static extern int ptrace(PtraceRequest request, int pid, IntPtr addr, IntPtr data);

    [DllImport("/usr/lib/system/libsystem_kernel.dylib")]
    private static extern int sysctl(int[] name, uint namelen, IntPtr oldp, IntPtr oldlenp, IntPtr newp, IntPtr newlen);

    private enum PtraceRequest
    {
        PtraceDenyAttach = 31
    }

    public static void PreventDebugging()
    {
        Log("DebuggerDetector: PreventDebugging method called");
        Log($"DebuggerDetector: Current Runtime.Arch = {Runtime.Arch}");

        if (Runtime.Arch == Arch.DEVICE)
        {
            Log("DebuggerDetector: Running on device, attempting to prevent debugging");
            try
            {
                int result = ptrace(PtraceRequest.PtraceDenyAttach, 0, IntPtr.Zero, IntPtr.Zero);
                Log($"DebuggerDetector: ptrace result = {result}");
            }
            catch (Exception ex)
            {
                Log($"DebuggerDetector: Exception in ptrace call - {ex.Message}");
            }
        }
        else
        {
            Log("DebuggerDetector: Not running on device, skipping ptrace call");
        }
    }

    public static bool IsDebuggerAttached()
    {
        Log("DebuggerDetector: IsDebuggerAttached method called");
        
        bool debuggerAttached = Debugger.IsAttached;
        Log($"DebuggerDetector: Debugger.IsAttached = {debuggerAttached}");
        
        bool sysctlCheck = CheckSysCtl();
        Log($"DebuggerDetector: CheckSysCtl result = {sysctlCheck}");
        
        bool exceptionCheck = CheckExceptionHandling();
        Log($"DebuggerDetector: CheckExceptionHandling result = {exceptionCheck}");
        
        bool result = debuggerAttached || sysctlCheck || exceptionCheck;
        Log($"DebuggerDetector: Final IsDebuggerAttached result = {result}");
        
        return result;
    }

    private static bool CheckSysCtl()
    {
        Log("DebuggerDetector: CheckSysCtl method called");
        int[] name = { 1, 14 }; // CTL_KERN, KERN_PROC, KERN_PROC_PID
        int[] info = new int[4];
        int size = 4 * sizeof(int);
        GCHandle handle = GCHandle.Alloc(info, GCHandleType.Pinned);
        
        try
        {
            int sysctlResult = sysctl(name, 2, handle.AddrOfPinnedObject(), (IntPtr)size, IntPtr.Zero, IntPtr.Zero);
            Log($"DebuggerDetector: sysctl result = {sysctlResult}");
            
            if (sysctlResult != 0)
            {
                Log("DebuggerDetector: sysctl call failed");
                return false;
            }
            
            bool isTraced = (info[0] & 0x800) != 0; // P_TRACED flag
            Log($"DebuggerDetector: P_TRACED flag = {isTraced}");
            return isTraced;
        }
        catch (Exception ex)
        {
            Log($"DebuggerDetector: Exception in CheckSysCtl - {ex.Message}");
            return false;
        }
        finally
        {
            handle.Free();
        }
    }

    private static bool CheckExceptionHandling()
    {
        Log("DebuggerDetector: CheckExceptionHandling method called");
        try
        {
            throw new Exception("Debugger check");
        }
        catch (Exception ex)
        {
            bool containsDebuggerBreak = ex.StackTrace.Contains("System.Diagnostics.Debugger.Break()");
            Log($"DebuggerDetector: Exception stack trace contains Debugger.Break() = {containsDebuggerBreak}");
            return containsDebuggerBreak;
        }
    }

    public static void StartContinuousDebuggerChecks(Action onDebuggerDetected, int checkIntervalMs = 1000)
    {
        Log($"DebuggerDetector: Starting continuous debugger checks with interval {checkIntervalMs}ms");
        Task.Run(async () =>
        {
            int checkCount = 0;
            while (true)
            {
                checkCount++;
                Log($"DebuggerDetector: Performing check #{checkCount}");
                
                if (IsDebuggerAttached())
                {
                    Log("DebuggerDetector: Debugger detected in continuous check!");
                    onDebuggerDetected?.Invoke();
                    break;
                }
                
                Log($"DebuggerDetector: Check #{checkCount} completed, no debugger detected");
                await Task.Delay(checkIntervalMs);
            }
        });
    }

    public static void LogAppOpening()
    {
        Log("DebuggerDetector: Application is opening");
        Log($"DebuggerDetector: Current date and time: {DateTime.Now}");
        Log($"DebuggerDetector: Operating System: {Environment.OSVersion}");
        Log($"DebuggerDetector: Device Model: {DeviceInfo.Model}");
        Log($"DebuggerDetector: Device Manufacturer: {DeviceInfo.Manufacturer}");
        Log($"DebuggerDetector: Device Name: {DeviceInfo.Name}");
        Log($"DebuggerDetector: Device Version: {DeviceInfo.VersionString}");
    }
}
