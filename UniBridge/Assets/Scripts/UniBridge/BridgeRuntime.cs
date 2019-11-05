using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace UniBridge {
    static class Internal {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void BridgeInitRuntime(UniBridgeGlue glue);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void BridgeDropRuntime();

        const string DYLIB_PATH = "/../../hello-world/target/debug/libHelloWorld.dylib";

        private static HotReload _internalDll = null;

        public static void ResetHotReload() {
            _internalDll.Dispose();
            _internalDll = null;
        }

        public static HotReload InternalDll =>
            _internalDll ?? (_internalDll = new HotReload(Application.dataPath + DYLIB_PATH));

#if UNITY_EDITOR
        public static void unibridge_init_runtime(UniBridgeGlue glue) {
            var f = Marshal.GetDelegateForFunctionPointer<BridgeInitRuntime>(
                                                                             InternalDll
                                                                                .FindSymbol("unibridge_init_runtime"));

            f(glue);
        }

        public static void unibridge_drop_runtime() {
            var f = Marshal.GetDelegateForFunctionPointer<BridgeDropRuntime>(
                                                                             InternalDll
                                                                                .FindSymbol("unibridge_drop_runtime"));

            f();
        }
#else
        [DllImport("HelloWorld", CallingConvention = CallingConvention.Cdecl)]
        public static extern void unibridge_init_runtime(UniBridgeGlue glue);

        [DllImport("HelloWorld", CallingConvention = CallingConvention.Cdecl)]
        public static extern void unibridge_drop_runtime();``
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    struct UniBridgeGlue {
        /* デリゲート型定義 */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UnityDebugLog(Slice<char> message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UniBridgePanicHandler();

        /* フィールド */

        private UniBridgePanicHandler _handlePanic;
        private UnityDebugLog         _errorLog;
        private UnityDebugLog         _warnLog;
        private UnityDebugLog         _infoLog;

        private InstancePool.NewInstanceDelegate     _newInstance;
        private InstancePool.DisposeInstanceDelegate _disposeInstance;
        private InstancePool.InvokeMethodDelegate    _invokeMethod;
        private InstancePool.InvokeAsDelegate        _invokeAs;

        /* 既定の実装 */

        [MonoPInvokeCallback(typeof(UniBridgePanicHandler))]
        public static void HandlePanic() {
            Debug.LogError("panic called from Rust.");

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        [MonoPInvokeCallback(typeof(UnityDebugLog))]
        private static void ErrorLog(Slice<char> message) {
            Debug.LogError(message.ToString());
        }

        [MonoPInvokeCallback(typeof(UnityDebugLog))]
        private static void WarnLog(Slice<char> message) {
            Debug.LogWarning(message.ToString());
        }

        [MonoPInvokeCallback(typeof(UnityDebugLog))]
        private static void InfoLog(Slice<char> message) {
            Debug.Log(message.ToString());
        }

        public static UniBridgeGlue CreateDefault() {
            return new UniBridgeGlue {
                _handlePanic     = HandlePanic,
                _errorLog        = ErrorLog,
                _warnLog         = WarnLog,
                _infoLog         = InfoLog,
                _newInstance     = InstancePool.NewInstance,
                _disposeInstance = InstancePool.DisposeInstance,
                _invokeMethod    = InstancePool.InvokeMethod,
                _invokeAs        = InstancePool.InvokeAs,
            };
        }
    }

    /// <summary>
    /// Unity上でRustのコードを実行するためのランタイムです。
    /// </summary>
    public class BridgeRuntime : MonoBehaviour {
        private static bool _onceInitialized = false;

        private void Awake() {
            // ランタイムの初期化を行う
            if (_onceInitialized)
                return;

            var glue = UniBridgeGlue.CreateDefault();

            Internal.unibridge_init_runtime(glue);
            _onceInitialized = true;
        }

        private void OnDestroy() {
            // ランタイムの破棄を行う
            _onceInitialized = false;

            Internal.unibridge_drop_runtime();
            Internal.ResetHotReload();
        }
    }
}
