using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace Radeoner
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IntRange
    {
        public int Min, Max, Step;
        public IntRange(int min, int max, int step) { Min = min; Max = max; Step = step; }
        public bool Contains(int value) { return Min <= Max && Step > 0 && value >= Min && value <= Max && ((long)value - Min) % Step == 0; }
        public override string ToString() { return L.T("range.description", Min, Max, Step); }
    }

    // ADLX exports use cdecl, interface methods stdcall; adlx_bool is one byte.
    internal static class Native
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibraryEx(string file, IntPtr reserved, uint flags);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        internal static extern IntPtr GetProcAddress(IntPtr module, string name);
        [DllImport("kernel32.dll")] internal static extern bool FreeLibrary(IntPtr module);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int QueryVersion(out ulong value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int Init(ulong version, out IntPtr system);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int Terminate();
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int Ref(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate uint Size(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int QueryInterface(IntPtr self, [MarshalAs(UnmanagedType.LPWStr)] string name, out IntPtr value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int GetPtr(IntPtr self, out IntPtr value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int GpuPtr(IntPtr self, IntPtr gpu, out IntPtr value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int At(IntPtr self, uint index, out IntPtr value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int GetInt(IntPtr self, out int value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int GetDouble(IntPtr self, out double value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int GetBool(IntPtr self, out byte value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int GpuBool(IntPtr self, IntPtr gpu, out byte value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int SetInt(IntPtr self, int value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int SetBool(IntPtr self, byte value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int GpuAction(IntPtr self, IntPtr gpu);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int GetRange(IntPtr self, out IntRange value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int GetRanges(IntPtr self, out IntRange first, out IntRange second);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int ValidateList(IntPtr self, IntPtr list, out int errorIndex);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] internal delegate int SetList(IntPtr self, IntPtr list);

        internal static T Function<T>(IntPtr pointer) where T : class
        {
            if (pointer == IntPtr.Zero) throw new InvalidOperationException(L.T("driver.086"));
            return (T)(object)Marshal.GetDelegateForFunctionPointer(pointer, typeof(T));
        }
        internal static T Method<T>(IntPtr obj, int slot) where T : class
        {
            if (obj == IntPtr.Zero) throw new InvalidOperationException(L.T("driver.087"));
            return Function<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(obj), slot * IntPtr.Size));
        }
        internal static string Error(int code)
        {
            string[] names = { "OK", "ALREADY_ENABLED", "ALREADY_INITIALIZED", "FAIL", "INVALID_ARGS", "BAD_VER", "UNKNOWN_INTERFACE", "TERMINATED", "ADL_INIT_ERROR", "NOT_FOUND", "INVALID_OBJECT", "ORPHAN_OBJECTS", "NOT_SUPPORTED", "PENDING_OPERATION", "GPU_INACTIVE", "GPU_IN_USE", "TIMEOUT_OPERATION", "NOT_ACTIVE", "RESET_NEEDED" };
            return "ADLX_" + (code >= 0 && code < names.Length ? names[code] : code.ToString());
        }
        internal static void Check(int code, string action)
        {
            if (code != 0) throw new InvalidOperationException(action + ": " + Error(code) +
                (code == 18 ? L.T("driver.088") : "."));
        }
    }

    // Every acquired interface is released before ADLXTerminate; the System interface is borrowed.
    internal sealed class AdlxObject : IDisposable
    {
        internal IntPtr Pointer { get; private set; }
        internal AdlxObject(IntPtr pointer) { Pointer = pointer; }
        internal T Method<T>(int slot) where T : class { return Native.Method<T>(Pointer, slot); }
        internal AdlxObject Query(string name)
        {
            IntPtr value;
            int rc = Method<Native.QueryInterface>(2)(Pointer, name, out value);
            if (rc != 0 || value == IntPtr.Zero) { if (value != IntPtr.Zero) new AdlxObject(value).Dispose(); return null; }
            return new AdlxObject(value);
        }
        internal AdlxObject Get(int slot)
        {
            IntPtr value; Native.Check(Method<Native.GetPtr>(slot)(Pointer, out value), L.T("driver.089"));
            if (value == IntPtr.Zero) throw new InvalidOperationException(L.T("driver.090"));
            return new AdlxObject(value);
        }
        internal AdlxObject At(uint index)
        {
            // ADLX typed lists have their typed At method at slot 11. Some runtime
            // versions reject the inherited generic At (slot 7) with UNKNOWN_INTERFACE.
            IntPtr value; Native.Check(Method<Native.At>(11)(Pointer, index, out value), L.T("driver.091") + index);
            if (value == IntPtr.Zero) throw new InvalidOperationException(L.T("driver.092"));
            return new AdlxObject(value);
        }
        internal int Int(int slot) { int v; Native.Check(Method<Native.GetInt>(slot)(Pointer, out v), L.T("driver.093")); return v; }
        internal bool Bool(int slot) { byte v; Native.Check(Method<Native.GetBool>(slot)(Pointer, out v), L.T("driver.094")); return v != 0; }
        internal string Text(int slot) { IntPtr v; Native.Check(Method<Native.GetPtr>(slot)(Pointer, out v), L.T("driver.095")); return Marshal.PtrToStringAnsi(v) ?? ""; }
        internal uint Count { get { return Method<Native.Size>(3)(Pointer); } }
        public void Dispose()
        {
            if (Pointer != IntPtr.Zero) { Method<Native.Ref>(1)(Pointer); Pointer = IntPtr.Zero; }
        }
    }

    internal sealed class AdlxLibrary : IDisposable
    {
        private IntPtr module;
        private Native.Terminate terminate;
        internal IntPtr System { get; private set; }
        internal string Version { get; private set; }
        internal string Path { get; private set; }
        internal AdlxLibrary()
        {
            if (!Environment.Is64BitProcess) throw new InvalidOperationException(L.T("driver.096"));
            Path = global::System.IO.Path.Combine(Environment.SystemDirectory, "amdadlx64.dll");
            // Never load a driver DLL from the current directory or the program's folder.
            module = Native.LoadLibraryEx(Path, IntPtr.Zero, 0x00000800); // LOAD_LIBRARY_SEARCH_SYSTEM32
            if (module == IntPtr.Zero) throw new InvalidOperationException(L.T("driver.097") + new Win32Exception(Marshal.GetLastWin32Error()).Message);
            try
            {
                ulong version;
                Native.Check(Export<Native.QueryVersion>("ADLXQueryFullVersion")(out version), L.T("driver.098"));
                Version = (version >> 48) + "." + ((version >> 32) & 65535) + "." + ((version >> 16) & 65535) + "." + (version & 65535);
                terminate = Export<Native.Terminate>("ADLXTerminate");
                IntPtr system;
                // Use the installed runtime's version: only stable base interfaces are consumed.
                Native.Check(Export<Native.Init>("ADLXInitialize")(version, out system), L.T("driver.099"));
                if (system == IntPtr.Zero) throw new InvalidOperationException(L.T("driver.100"));
                System = system;
            }
            catch { Dispose(); throw; }
        }
        private T Export<T>(string name) where T : class { return Native.Function<T>(Native.GetProcAddress(module, name)); }
        public void Dispose()
        {
            if (System != IntPtr.Zero) { terminate(); System = IntPtr.Zero; }
            if (module != IntPtr.Zero) { Native.FreeLibrary(module); module = IntPtr.Zero; }
        }
    }
}
