using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace UniBridge {
    public static class InstancePool {
        /* デリゲート型定義 */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt64 NewInstanceDelegate(Slice<char> className, Slice<UInt64> args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DisposeInstanceDelegate(UInt64 id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt64 InvokeMethodDelegate(UInt64 id, Slice<char> methodName, Slice<UInt64> args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt64 InvokeAsDelegate(UInt64        id, Slice<char> className, Slice<char> methodName,
                                                Slice<UInt64> args);

        private static Dictionary<UInt64, (UInt64, object)> _instances       = new Dictionary<UInt64, (UInt64, object)>();
        private static UInt64                               _instanceCounter = 1;
        private static Stack<UInt64>                        _unusedInstance  = new Stack<UInt64>();

        public static object GetInstance(UInt64 id) {
            if (id == 0) {
                return null;
            }

            return _instances.TryGetValue(id, out var o) ? o.Item2 : null;
        }

        // Unity C#側から呼び出すメソッド
        public static UInt64 AppendInstance(object instance) {
            // ヌル特殊化
            if (instance == null) {
                return 0;
            }

            var id = _unusedInstance.Count == 0 ? _instanceCounter++ : _unusedInstance.Pop();
            _instances[id] = (1, instance);

            return id;
        }

        public static UInt64 CloneInstance(UInt64 id) {
            if (id == 0 || !_instances.ContainsKey(id)) {
                return 0;
            }
            
            var (cnt, o) = _instances[id];
            _instances[id] = (cnt + 1, o);

            return id;
        }

        public static void DisposeAllInstances() {
            _instanceCounter = 1;
            _unusedInstance.Clear();
            _instances.Clear();
        }

        // Rust側から呼び出すメソッド
        [MonoPInvokeCallback(typeof(NewInstanceDelegate))]
        public static UInt64 NewInstance(Slice<char> className, Slice<UInt64> args) {
            var args1 = args.Select(GetInstance)
                            .ToArray();

            var ty = Type.GetType(className.ToString());
            var c =
                ty?.GetConstructor(args1.Length == 0 ? Type.EmptyTypes : args1.Select(o => o.GetType()).ToArray());
            var instance = c?.Invoke(args1);

            return AppendInstance(instance);
        }

        [MonoPInvokeCallback(typeof(DisposeInstanceDelegate))]
        public static void DisposeInstance(UInt64 id) {
            if (id == 0 || !_instances.ContainsKey(id)) {
                return;
            }
            
            var (cnt, obj)   = _instances[id];
            cnt--;

            if (cnt == 0) {
                _unusedInstance.Push(id);
                object o = _instances.Remove(id);
            } else {
                _instances[id] = (cnt, obj);
            }
        }

        /* */
        
        [MonoPInvokeCallback(typeof(InvokeMethodDelegate))]
        public static UInt64 InvokeMethod(UInt64 id, Slice<char> methodName, Slice<UInt64> args) {
            var instance = GetInstance(id);
            var ty       = instance?.GetType();
            var args1 = args.Select(GetInstance)
                            .ToArray();


            var method = ty?.GetMethod(methodName.ToString(),
                                       args1.Length == 0 ? Type.EmptyTypes : args1.Select(o => o.GetType()).ToArray());

            return AppendInstance(method?.Invoke(instance, args1));
        }

        [MonoPInvokeCallback(typeof(InvokeAsDelegate))]
        public static UInt64 InvokeAs(UInt64 id, Slice<char> className, Slice<char> methodName, Slice<UInt64> args) {
            // id == 0 （ヌル）の時は、静的メソッド呼び出しを行う
            var ty = Type.GetType(className.ToString());

            if (ty == null) {
                Debug.LogWarning("type is null");
            }

            var args1 = args.Select(GetInstance)
                            .ToArray();

            if (id == 0) {
                var res = ty?.InvokeMember(methodName.ToString(), System.Reflection.BindingFlags.InvokeMethod,
                                           null, null, args1);
                return AppendInstance(res);
            }

            var instance = GetInstance(id);
            var method = ty?.GetMethod(methodName.ToString(),
                                       args1.Length == 0 ? Type.EmptyTypes : args1.Select(o => o.GetType()).ToArray());

            if (method == null) {
                Debug.LogWarning("method is null");
            }

            return AppendInstance(method?.Invoke(instance, args1));
        }
    }
}
