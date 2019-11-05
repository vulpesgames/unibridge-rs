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
    public static class Internal {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void BridgeInitRuntime(UniBridgeGlue glue);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void BridgeDropRuntime();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate UInt64 RustInvoke(void * rust, UInt64 context, Slice<char> name, Slice<UInt64> args);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate void* NewFerris();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate void KillFerris(void* ptr);
        
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        const string DYLIB_PATH = "/../../target/debug/libHelloWorld.dylib";
#else
		const string DYLIB_PATH = "/../../target/debug/HelloWorld.dll";
#endif
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
        
        //
        public static unsafe UInt64 unibridge_invoke(void * rust, UInt64 context, Slice<char> name, Slice<UInt64> args) {
            var f = Marshal.GetDelegateForFunctionPointer<RustInvoke>(
                                                                      InternalDll
                                                                         .FindSymbol("unibridge_invoke"));

            return f(rust, context, name, args);
        }
        
        public static unsafe void* new_ferris() {
            var f = Marshal.GetDelegateForFunctionPointer<NewFerris>(
                                                                      InternalDll
                                                                         .FindSymbol("new_ferris"));

            return f();
        }
        
        public static unsafe void kill_ferris(void *ferris) {
            var f = Marshal.GetDelegateForFunctionPointer<KillFerris>(
                                                                     InternalDll
                                                                        .FindSymbol("kill_ferris"));

            f(ferris);
        }

#else
        [DllImport("HelloWorld", CallingConvention = CallingConvention.Cdecl)]
        public static extern void unibridge_init_runtime(UniBridgeGlue glue);

        [DllImport("HelloWorld", CallingConvention = CallingConvention.Cdecl)]
        public static extern void unibridge_drop_runtime();``
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UniBridgeGlue {
        /* デリゲート型定義 */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UnityDebugLog(Slice<char> message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UniBridgePanicHandler();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate UInt64 UniBridgeToString(Slice<char> str);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate UInt64 UniBridgeToF32(float x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate float UniBridgeTryF32(UInt64 x);

        /* フィールド */

        private UniBridgePanicHandler _handlePanic;
        private UnityDebugLog         _errorLog;
        private UnityDebugLog         _warnLog;
        private UnityDebugLog         _infoLog;

        private InstancePool.NewInstanceDelegate     _newInstance;
        private InstancePool.DisposeInstanceDelegate _disposeInstance;
        private InstancePool.InvokeMethodDelegate    _invokeMethod;
        private InstancePool.InvokeAsDelegate        _invokeAs;

        private UniBridgeToString _toString;
        private UniBridgeToF32    _toF32;
        private UniBridgeTryF32   _tryF32;

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
                // エラーおよびログ機能
                _handlePanic = HandlePanic,
                _errorLog    = ErrorLog,
                _warnLog     = WarnLog,
                _infoLog     = InfoLog,
                // メソッド呼び出しおよびインスタンス生成・破棄
                _newInstance     = InstancePool.NewInstance,
                _disposeInstance = InstancePool.DisposeInstance,
                _invokeMethod    = InstancePool.InvokeMethod,
                _invokeAs        = InstancePool.InvokeAs,
                // プリミティブ型 <-> オブジェクト型変換
                _toString = s => InstancePool.AppendInstance(s.ToString()),
                _toF32    = x => InstancePool.AppendInstance(x),
                _tryF32   = x => (float) InstancePool.GetInstance(x),
            };
        }
    }

    /// <summary>
    /// Unity上でRustのコードを実行するためのランタイムです。
    /// </summary>
    public class BridgeRuntime : MonoBehaviour {
        private static bool _onceInitialized = false;
        
        static BridgeRuntime _instance = null;

        public static BridgeRuntime Instance {
            get {
                if (_instance == null) {
                    var previous = FindObjectOfType<BridgeRuntime>();
                    if (previous != null) {
                        _instance = previous;
                    } else {
                        var obj = new GameObject ("UniBridge Runtime");
                        _instance = obj.AddComponent<BridgeRuntime> ();
                        DontDestroyOnLoad (obj);
                        obj.hideFlags = HideFlags.HideInHierarchy;
                    }
                }
                return _instance;
            }
        }

        public static void InitializeRuntime() {
            if (_onceInitialized)
                return;

            _onceInitialized = true;
            Instance.RuntimeEntry();
        }

        public static RustInstance GetRustInstance(string name) {
            return null;
        }

        private void Awake() {
            // ランタイムの初期化を行う
            if (_onceInitialized)
                return;
            
            // TODO:
        }

        private void RuntimeEntry() {
            var glue = UniBridgeGlue.CreateDefault();

            Internal.unibridge_init_runtime(glue);
        }

        private void OnDestroy() {
            // ランタイムの破棄を行う
            _onceInitialized = false;

            Internal.unibridge_drop_runtime();
            Internal.ResetHotReload();
        }
    }
}
