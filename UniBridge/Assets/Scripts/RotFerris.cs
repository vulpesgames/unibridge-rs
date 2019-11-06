// not (currently) generated by UniBridge.

using System;
using System.Linq;
using UnityEngine;
using UniBridge;

public class RotFerris : MonoBehaviour {
    [SerializeField] GameObject ferris = null;

    private unsafe void* _rustInstance;

    private void Awake() {
        // ランタイムの初期化を行う
        BridgeRuntime.InitializeRuntime();

        // Rust側のインスタンス（RotFerris）を取得する
        unsafe {
            _rustInstance = Internal.new_ferris(InstancePool.AppendInstance(this));
        }
    }

    private void OnDestroy() {
        unsafe {
            Internal.kill_ferris(_rustInstance);
        }
    }

    private object _InvokeRustFunction(string methodName, params object[] args) {
        // Rust側のインスタンスの関数を呼び出す
        var methodName1 = System.Text.Encoding.UTF8.GetBytes(methodName);
        var args1 = args.Select(InstancePool.AppendInstance)
                       .ToArray();

        unsafe {
            fixed (UInt64* a = args1)
            fixed (byte* b = methodName1) {
                var res = Internal.unibridge_invoke(_rustInstance,
                                                     new Slice<char>((char*) b, (UIntPtr) methodName1.Length),
                                                     new Slice<UInt64>(a, (UIntPtr) args1.Length));

                var res1 = InstancePool.GetInstance(res);
                InstancePool.DisposeInstance(res);

                return res1;
            }
        }
    }

    private void Start() {
        _InvokeRustFunction("Start");
    }

    private void Update() {
        _InvokeRustFunction("Update");
    }

    public void SetFerrisRotation(float rot) {
        ferris.transform.rotation = Quaternion.Euler(0, 0, rot);
    }
    
    public void TestVector(Vector3 v) {
        Debug.Log(v);
    }
}
