using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UniBridge {
    /// <summary>
    /// Unity上でRustのコードを実行するためのランタイムです。
    /// </summary>
    public class BridgeRuntime : MonoBehaviour {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UnityDebugErrorLog(Slice<char> message);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void UnityDebugInfoLog(Slice<char> message);

        private bool _onceInitialized = false;
        private void Awake() {
            // ランタイムの初期化を行う
        }
    }
}
