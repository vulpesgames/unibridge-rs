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
                InternalDll.FindSymbol("unibridge_init_runtime"));

            f(glue);
        }

        public static void unibridge_drop_runtime() {
            var f = Marshal.GetDelegateForFunctionPointer<BridgeDropRuntime>(
                InternalDll.FindSymbol("unibridge_drop_runtime"));

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
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UnityDebugLog(Slice<char> message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UniBridgePanicHandler();

        private UniBridgePanicHandler _handlePanic;
        private UnityDebugLog         _errorLog;
        private UnityDebugLog         _warnLog;
        private UnityDebugLog         _infoLog;

        [MonoPInvokeCallback(typeof(UniBridgePanicHandler))]
        public static void HandlePanic() {
            Debug.LogError("panic called from Rust runtime.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
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
                _handlePanic = HandlePanic,
                _errorLog    = ErrorLog,
                _warnLog     = WarnLog,
                _infoLog     = InfoLog,
            };
        }
    }

    /// <summary>
    /// Unity上でRustのコードを実行するためのランタイムです。
    /// </summary>
    public class BridgeRuntime : MonoBehaviour {
        private bool _onceInitialized = false;

        private readonly Dictionary<UInt64, object> _instances       = new Dictionary<UInt64, object>();
        private          UInt64                     _instanceCounter = 1;
        private readonly Stack<UInt64>              _unusedInstance  = new Stack<UInt64>();

        private object GetInstance(UInt64 id) {
            if (id == 0) {
                return null;
            }

            return _instances.TryGetValue(id, out var o) ? o : null;
        }

        private UInt64 AppendInstance(object instance) {
            // ヌル特殊化
            if (instance == null) {
                return 0;
            }

            var id = _unusedInstance.Count == 0 ? _instanceCounter++ : _unusedInstance.Pop();
            _instances[id] = instance;

            return id;
        }

        private void DisposeInstance(UInt64 id) {
            _unusedInstance.Push(id);
            object o = _instances.Remove(id);
        }

        private void DisposeAllInstances() {
            _instanceCounter = 1;
            _unusedInstance.Clear();
            _instances.Clear();
        }

        private UInt64 NewInstance(Slice<char> className, Slice<UInt64> args) {
            var args1 = args.ToArray()
                            .Select(GetInstance)
                            .ToArray();

            var ty = Type.GetType(className.ToString());
            var c =
                ty?.GetConstructor(args1.Length == 0 ? Type.EmptyTypes : args1.Select(o => o.GetType()).ToArray());
            var instance = c?.Invoke(args1);

            return AppendInstance(instance);
        }

        private UInt64 InvokeMethod(UInt64 id, Slice<char> methodName, Slice<UInt64> args) {
            var instance = GetInstance(id);
            var ty       = instance?.GetType();
            var args1 = args.ToArray()
                            .Select(GetInstance)
                            .ToArray();


            var method = ty?.GetMethod(methodName.ToString(),
                                       args1.Length == 0 ? Type.EmptyTypes : args1.Select(o => o.GetType()).ToArray());

            return AppendInstance(method?.Invoke(instance, args1));
        }

        private UInt64 InvokeAs(UInt64 id, Slice<char> className, Slice<char> methodName, Slice<UInt64> args) {
            var instance = GetInstance(id);
            var ty       = Type.GetType(className.ToString());
            var args1 = args.ToArray()
                            .Select(GetInstance)
                            .ToArray();


            var method = ty?.GetMethod(methodName.ToString(),
                                       args1.Length == 0 ? Type.EmptyTypes : args1.Select(o => o.GetType()).ToArray());

            return AppendInstance(method?.Invoke(instance, args1));
        }

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
