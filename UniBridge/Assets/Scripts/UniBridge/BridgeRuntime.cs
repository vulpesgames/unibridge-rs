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
        unsafe delegate UInt64 RustInvoke(void* rust, Slice<char> name, Slice<UInt64> args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate void* NewFerris(UInt64 ctx);

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

#if UNITY_IOS || UNITY_ANDROID
        const string LIB_PATH = "__Internal";
#else
        const string LIB_PATH = "HelloWorld";
#endif

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
        public static unsafe UInt64 unibridge_invoke(void* rust, Slice<char> name, Slice<UInt64> args) {
            var f = Marshal.GetDelegateForFunctionPointer<RustInvoke>(
                                                                      InternalDll
                                                                         .FindSymbol("unibridge_invoke"));

            return f(rust, name, args);
        }

        public static unsafe void* new_ferris(UInt64 ctx) {
            var f = Marshal.GetDelegateForFunctionPointer<NewFerris>(
                                                                     InternalDll
                                                                        .FindSymbol("new_ferris"));

            return f(ctx);
        }

        public static unsafe void kill_ferris(void* ferris) {
            var f = Marshal.GetDelegateForFunctionPointer<KillFerris>(
                                                                      InternalDll
                                                                         .FindSymbol("kill_ferris"));

            f(ferris);
        }

#else
        [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void unibridge_init_runtime(UniBridgeGlue glue);

        [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void unibridge_drop_runtime();

        [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void unibridge_invoke(void* rust, Slice<char> name, Slice<UInt64> args);

        [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void* new_ferris();

        [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void kill_ferris(void* ferris);
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UniBridgeGlue {
        [StructLayout(LayoutKind.Sequential)]
        private struct TypeCastBool {
            private bool success;
            private bool value;

            public TypeCastBool(bool success, bool value) {
                this.success = success;
                this.value   = value;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TypeCastFloat {
            private bool  success;
            private float value;

            public TypeCastFloat(bool success, float value) {
                this.success = success;
                this.value   = value;
            }
        }

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
        delegate TypeCastBool UniBridgeTryBool(UInt64 x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate TypeCastFloat UniBridgeTryFloat(UInt64 x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate UInt64 UniBridgeClone(UInt64 id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate UInt64 UniBridgeSizedBytes(Slice<byte> ptr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate UInt64 UniBridgeGet(UInt64 id, Slice<char> className, Slice<char> name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool UniBridgeSet(UInt64 id, Slice<char> className, Slice<char> name, UInt64 value);

        /* フィールド */

        private UniBridgePanicHandler _handlePanic;
        private UnityDebugLog         _errorLog;
        private UnityDebugLog         _warnLog;
        private UnityDebugLog         _infoLog;

        private InstancePool.NewInstanceDelegate     _newInstance;
        private InstancePool.DisposeInstanceDelegate _disposeInstance;
        private InstancePool.InvokeMethodDelegate    _invokeMethod;
        private InstancePool.InvokeAsDelegate        _invokeAs;
        private UniBridgeClone                       _clone;
        private UniBridgeGet                         _getProperty;
        private UniBridgeGet                         _getField;

        private UniBridgeSet                         _setProperty;
        private UniBridgeSet                         _setField;

        private UniBridgeSizedBytes _sizedBytes;

        private UniBridgeToString _toString;
        private UniBridgeToF32    _toF32;
        private UniBridgeTryFloat _tryF32;
        private UniBridgeTryBool  _tryBool;

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

        private static TypeCastFloat TryCastFloat(UInt64 id) {
            try {
                return new TypeCastFloat(true, (float) InstancePool.GetInstance(id));
            } catch (InvalidCastException _) {
                return new TypeCastFloat(false, default(float));
            }
        }

        private static TypeCastBool TryCastBool(UInt64 id) {
            try {
                return new TypeCastBool(true, (bool) InstancePool.GetInstance(id));
            } catch (InvalidCastException _) {
                return new TypeCastBool(false, default(bool));
            }
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
                _clone           = InstancePool.CloneInstance,
                _getProperty     = (UInt64 id, Slice<char> className, Slice<char> propertyName) => {
                    var v = InstancePool.GetInstance(id);
                    var ty = v?.GetType() ?? Type.GetType(className.ToString());
                    var prop = propertyName.ToString();
                    var res = ty.GetProperty(prop);

                    return InstancePool.AppendInstance(
                        res?.GetValue(v)
                    );
                },
                _getField        = (UInt64 id, Slice<char> className, Slice<char> fieldName) => {
                    var v = InstancePool.GetInstance(id);
                    var ty = v?.GetType() ?? Type.GetType(className.ToString());
                    var prop = fieldName.ToString();
                    var res = ty.GetField(prop);

                    return InstancePool.AppendInstance(
                        res?.GetValue(v)
                    );
                },
                _setProperty     = (UInt64 id, Slice<char> className, Slice<char> propertyName, UInt64 value) => {
                    var v = InstancePool.GetInstance(id);
                    var ty = v?.GetType() ?? Type.GetType(className.ToString());
                    var prop = propertyName.ToString();
                    var res = ty.GetProperty(prop);

                    res?.SetValue(v, InstancePool.GetInstance(value));

                    return true;
                },
                _setField        = (UInt64 id, Slice<char> className, Slice<char> fieldName, UInt64 value) => {
                    var v = InstancePool.GetInstance(id);
                    var ty = v?.GetType() ?? Type.GetType(className.ToString());
                    var prop = fieldName.ToString();
                    var res = ty.GetField(prop);

                    res?.SetValue(v, InstancePool.GetInstance(value));

                    return true;
                },
                // 特殊キャスト
                _sizedBytes = b => InstancePool.AppendInstance(b.ToArray()),
                // プリミティブ型 <-> オブジェクト型変換
                _toString = s => InstancePool.AppendInstance(s.ToString()),
                _toF32    = x => InstancePool.AppendInstance(x),
                _tryF32   = TryCastFloat,
                _tryBool  = TryCastBool,
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
                        var obj = new GameObject("UniBridge Runtime");
                        _instance = obj.AddComponent<BridgeRuntime>();
                        DontDestroyOnLoad(obj);
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
