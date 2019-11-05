using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

#if UNITY_EDITOR_WIN
using Microsoft.Win32.SafeHandles;
#endif

namespace UniBridge {
    /// <summary>
    /// UniBridge上でDLLをホットリロードするためのクラスです。
    /// </summary>
    public class HotReload : IDisposable {
#if UNITY_EDITOR_OSX
        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern int dlclose(IntPtr handle);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr dlerror();

        private static IntPtr RTLD_NEXT      = new IntPtr(-1);
        private static IntPtr RTLD_DEFAULT   = new IntPtr(-2);
        private static IntPtr RTLD_SELF      = new IntPtr(-3);
        private static IntPtr RTLD_MAIN_ONLY = new IntPtr(-5);
        private const  int    RTLD_LAZY      = 0x01;
        private const  int    RTLD_NOW       = 0x02;
        private const  int    RTLD_LOCAL     = 0x04;
        private const  int    RTLD_GLOBAL    = 0x08;
        private const  int    RTLD_NOLOAD    = 0x10;
        private const  int    RTLD_NODELETE  = 0x80;
        private const  int    RTLD_FIRST     = 0x100;

        private IntPtr _dlHandler = IntPtr.Zero;

        private string _tempFile = null;

        public HotReload(string path) {
            var randNum = Random.Range(100000, 999999);

            _tempFile = path + ".tmp." + randNum + ".dylib";
            File.Copy(path, _tempFile);

            var installNameTool = new ProcessStartInfo {
                FileName               = "/usr/bin/install_name_tool",
                Arguments              = $"-id /tmp/UniBridge.{randNum}.dylib \"{_tempFile}\"",
                RedirectStandardError  = true,
                RedirectStandardOutput = true,
                UseShellExecute        = false,
            };
            using (var p = Process.Start(installNameTool)) {
                p.WaitForExit();

                if (p.ExitCode != 0) {
                    throw new Exception("failed to reload dylib");
                }
            }

            if ((_dlHandler = dlopen(_tempFile, RTLD_NOW | RTLD_GLOBAL)) == IntPtr.Zero) {
                throw new DllNotFoundException($"failed to load DLL \"{path}\": {Marshal.PtrToStringAnsi(dlerror())}");
            }
        }

        public IntPtr FindSymbol(string symbol) {
            var sym = dlsym(_dlHandler, symbol);

            /* while (true) {
                var tmp = dlsym(RTLD_NEXT, symbol);
                
                if (tmp == IntPtr.Zero || tmp == sym) {
                    break;
                }

                sym = tmp;
            } */

            if (sym == IntPtr.Zero) {
                throw new KeyNotFoundException(
                    $"failed to load symbol \"{symbol}\": {Marshal.PtrToStringAnsi(dlerror())}");
            }

            return sym;
        }

#elif UNITY_EDITOR_WIN
        // WindowsのUnityEditor向けの実装
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern LibraryHandle LoadLibrary(string lpLibFileName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hLibModule);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(LibraryHandle hModule, string lpProcName);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        sealed class LibraryHandle : SafeHandleZeroOrMinusOneIsInvalid {
            public LibraryHandle() : base(true) { }

            protected override bool ReleaseHandle() {
                return FreeLibrary(this.handle);
            }
        }

        private string        _tempFile = null;
        private LibraryHandle _handle = null;

        public HotReload(string path) {
            var randNum = Random.Range(100000, 999999);

            _tempFile = path + ".tmp." + randNum + ".dll";
            File.Copy(path, _tempFile);

            var h = LoadLibrary(_tempFile);
            if (h.IsInvalid) {
                throw new Exception($"failed to load DLL");
            }

            _handle = h;
        }

        public IntPtr FindSymbol(string symbol) {
            return GetProcAddress(_handle, symbol);
        }
#endif

        public void Dispose() {
#if UNITY_EDITOR_OSX
            if (_dlHandler == IntPtr.Zero) {
                return;
            }

            // 読み込んだDLLを破棄する
            File.Delete(_tempFile);

            if (dlclose(_dlHandler) != 0) {
                throw new Exception($"failed to unload DLL: {Marshal.PtrToStringAnsi(dlerror())}");
            }

            _dlHandler = IntPtr.Zero;
#elif UNITY_EDITOR_WIN
            _handle?.Dispose();

            // 読み込んだDLLを破棄する
            File.Delete(_tempFile);
#endif
        }
    }
}
