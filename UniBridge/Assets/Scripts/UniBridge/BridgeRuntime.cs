using System;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniBridge {
    static class Internal {
        [DllImport("HelloWorld")]
        public static extern void unibridge_init_runtime(UniBridgeGlue glue);

        [DllImport("HelloWorld")]
        public static extern void unibridge_drop_runtime();
    }
    
    [StructLayout(LayoutKind.Sequential)]
    struct UniBridgeGlue {
        delegate void UnityDebugLog(Slice<char> message);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UniBridgePanicHandler();

        private UniBridgePanicHandler _handlePanic;
        private UnityDebugLog _errorLog;
        private UnityDebugLog _warnLog;
        private UnityDebugLog _infoLog;
        
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
            return new UniBridgeGlue
            {
                _handlePanic = HandlePanic,
                _errorLog = ErrorLog,
                _warnLog = WarnLog,
                _infoLog = InfoLog,
            };
        }
    }

    /// <summary>
    /// Unity上でRustのコードを実行するためのランタイムです。
    /// </summary>
    public class BridgeRuntime : MonoBehaviour {
        private bool _onceInitialized = false;

        private void Awake() {
            // ランタイムの初期化を行う
            if (_onceInitialized)
                return;
            
            Internal.unibridge_init_runtime(UniBridgeGlue.CreateDefault());
            _onceInitialized = true;
        }

        private void OnDestroy() {
            // ランタイムの破棄を行う
            _onceInitialized = false;

            Internal.unibridge_drop_runtime();
        }
    }
}
