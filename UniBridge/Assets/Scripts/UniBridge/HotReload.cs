using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

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

        public HotReload(string path) {
            if ((_dlHandler = dlopen(path, RTLD_LAZY | RTLD_LOCAL)) == IntPtr.Zero) {
                throw new DllNotFoundException($"failed to load DLL \"{path}\": ${Marshal.PtrToStringAnsi(dlerror())}");
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
                    $"failed to load symbol \"{symbol}\": ${Marshal.PtrToStringAnsi(dlerror())}");
            }

            return sym;
        }
#endif

        public void Dispose() {
            if (_dlHandler == IntPtr.Zero) {
                return;
            }

#if UNITY_EDITOR_OSX
            // 読み込んだDLLを破棄する
            if (dlclose(_dlHandler) != 0) {
                throw new Exception($"failed to unload DLL: ${Marshal.PtrToStringAnsi(dlerror())}");
            }

            _dlHandler = IntPtr.Zero;
#endif
        }
    }
}
